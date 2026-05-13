
using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class ObCardPower : CuteSakikoModPower
{
    private const int ExtraReplay = 1;
    private readonly HashSet<CardModel> _modifiedCards = new();
    private readonly Dictionary<CardModel, int> _originalCosts = new();
    private bool _isRemoving;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Sakiforget);
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Memory);
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        await ModifyExistingCards();
    }

    // 立即修改玩家当前所有卡牌
    private async Task ModifyExistingCards()
    {
        if (Owner?.Player == null) return;
        var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust };
        foreach (var pileType in piles)
        {
            var pile = pileType.GetPile(Owner.Player);
            if (pile == null) continue;
            foreach (var card in pile.Cards)
                if (!_modifiedCards.Contains(card))
                    ApplyModificationsToCard(card);
        }

        await Task.CompletedTask;
    }

    // 新卡牌进入战斗时自动修改
    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (_isRemoving) return;
        if (card.Owner != Owner.Player) return;
        if (!_modifiedCards.Contains(card))
            ApplyModificationsToCard(card);
    }

    // 打出修改过的卡牌时：减压力并立即遗忘
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_isRemoving) return;
        var card = cardPlay.Card;
        if (card.Owner?.Creature != Owner) return;

        if (_modifiedCards.Contains(card))
        {
            // 减少 1 层压力
            var pressure = Owner.GetPower<PressurePower>();
            if (pressure != null && pressure.Amount > 0)
                await PowerCmd.ModifyAmount(choiceContext, pressure, -1, Owner, card);

            // 遗忘该卡牌（会触发记忆堆清理等后续效果）
             MemoryCmd.Forget(choiceContext, new[] { card }, null);

            // 从修改记录中移除，避免后续恢复时找不到
            _modifiedCards.Remove(card);
            _originalCosts.Remove(card);
        }
    }

    // 回合结束时检查压力，若为 0 则移除能力并恢复卡牌
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (_isRemoving) return;
        if (side != Owner.Side) return;

        var pressure = Owner.GetPower<PressurePower>();
        if (pressure == null || pressure.Amount == 0)
            await RemovePowerAndRestore();
    }

    private void ApplyModificationsToCard(CardModel card)
    {
        // 记录原始费用
        if (!_originalCosts.ContainsKey(card))
        {
            var originalCost = card.EnergyCost.GetWithModifiers(CostModifiers.None);
            _originalCosts[card] = originalCost;
        }

        // 设置费用为 1
        card.EnergyCost.SetThisCombat(1, true);

        // 重放次数 +1
        card.BaseReplayCount += ExtraReplay;

        _modifiedCards.Add(card);
    }

    private async Task RemovePowerAndRestore()
    {
        if (_isRemoving) return;
        _isRemoving = true;

        if (Owner?.Player != null)
        {
            var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust };
            foreach (var pileType in piles)
            {
                var pile = pileType.GetPile(Owner.Player);
                if (pile == null) continue;
                foreach (var card in pile.Cards)
                {
                    if (!_modifiedCards.Contains(card)) continue;

                    // 恢复费用
                    if (_originalCosts.TryGetValue(card, out var originalCost))
                    {
                        var energyCost = card.EnergyCost;

                        // 清空本地修改器
                        var modifiersField = energyCost.GetType()
                            .GetField("_localModifiers", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (modifiersField?.GetValue(energyCost) is System.Collections.IList list)
                            list.Clear();

                        // 重置基础费用
                        var baseField = energyCost.GetType()
                            .GetField("_base", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (baseField != null)
                            baseField.SetValue(energyCost, originalCost);

                        // 通知 UI 刷新
                        card.InvokeEnergyCostChanged();
                    }

                    // 恢复重放次数
                    card.BaseReplayCount -= ExtraReplay;

                    _modifiedCards.Remove(card);
                    _originalCosts.Remove(card);
                }
            }
        }

        _modifiedCards.Clear();
        _originalCosts.Clear();
        await PowerCmd.Remove(this);
    }
}