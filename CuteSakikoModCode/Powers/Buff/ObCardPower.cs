
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;


namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class ObCardPower : CuteSakikoModPower
{
    private const int ExtraReplay = 1;
    private readonly HashSet<CardModel> _modifiedCards = new();
    private readonly Dictionary<CardModel, int> _originalCosts = new();
    private readonly Dictionary<CardModel, bool> _hadExhaustKeyword = new();
    private bool _isRemoving;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<PressurePower>();
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

    // 只遍历一次现有卡牌
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

    // 监听新卡牌进入战斗（例如抽牌、生成）
    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (_isRemoving) return;
        if (card.Owner != Owner.Player) return;
        if (!_modifiedCards.Contains(card))
            ApplyModificationsToCard(card);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_isRemoving) return;
        var card = cardPlay.Card;
        if (card.Owner?.Creature != Owner) return;

        // 减少压力
        var pressure = Owner.GetPower<PressurePower>();
        if (pressure != null && pressure.Amount > 0)
            await PowerCmd.ModifyAmount(choiceContext,pressure, -1, Owner, card);

        // 新打出的卡牌已在手牌中，且已被修改过，无需再全量遍历
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (_isRemoving) return;
        if (side != Owner.Side) return;

        var pressure = Owner.GetPower<PressurePower>();
        if (pressure == null || pressure.Amount == 0)
            await RemovePowerAndRestore();
        // 不再调用 ModifyAllCardsInAllPiles
    }

    private void ApplyModificationsToCard(CardModel card)
    {
        if (!_originalCosts.ContainsKey(card))
        {
            var originalCost = card.EnergyCost.GetWithModifiers(CostModifiers.None);
            _originalCosts[card] = originalCost;
        }
        if (!_hadExhaustKeyword.ContainsKey(card))
            _hadExhaustKeyword[card] = card.Keywords.Contains(CardKeyword.Exhaust);

        card.EnergyCost.SetThisCombat(1, true);
        card.BaseReplayCount += ExtraReplay;
        if (!card.Keywords.Contains(CardKeyword.Exhaust))
            card.AddKeyword(CardKeyword.Exhaust);
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
                    if (_originalCosts.TryGetValue(card, out var originalCost))
                        card.EnergyCost.SetThisCombat(originalCost, true);
                    if (_modifiedCards.Contains(card))
                        card.BaseReplayCount -= ExtraReplay;
                    if (_hadExhaustKeyword.TryGetValue(card, out var hadExhaust) && !hadExhaust)
                        card.RemoveKeyword(CardKeyword.Exhaust);
                }
            }
        }
        _modifiedCards.Clear();
        _originalCosts.Clear();
        _hadExhaustKeyword.Clear();
        await PowerCmd.Remove(this);
    }
}