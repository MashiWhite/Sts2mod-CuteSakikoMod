using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Basic;

[RegisterCharacterStarterRelic(typeof(CuteSaki))]
[RegisterTouchOfOrobasRefinement(typeof(PostItNote))]
public class KabutoNote : CuteSakikoModRelic  // ← 去掉 sealed
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<string> RegisteredKeywordIds => [CutesakiKeywords.Memorysaki];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side == Owner.Creature.Side && combatState.RoundNumber == 1)
        {
            await PowerCmd.Apply<PressurePower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 3, Owner.Creature, null);
            Flash();
        }
    }

    /// <summary>
    ///     右键打开回忆卡牌查看界面（复用原版牌堆查看系统）
    /// </summary>
    public void OpenMemoryLibrary()
    {
        var player = Owner;
        if (player == null) return;

        var combatState = player.PlayerCombatState;
        if (combatState == null) return;

        var exhaustedIds = SakiMemoryManager.Instance.ExhaustedMemoryIds;
        var seenIds = new HashSet<ModelId>();
        var cardsToShow = new List<CardModel>();

        bool ShouldShow(CardModel card)
        {
            if (card == null) return false;
            if (exhaustedIds.Contains(card.Id)) return false;
            // 排除升级卡牌
            if (card.IsUpgraded) return false;
            if (seenIds.Contains(card.Id)) return false;
            return true;
        }

        // 1. 原始模板库中的回忆卡（基础版）
        foreach (var template in ModelDb.AllCards)
        {
            // 直接检查模板是否带有关键字（模拟 HasModKeyword）
            if (template.HasModKeyword(CutesakiKeywords.Memory))
            {
                var instance = player.RunState.CreateCard(template, player);
                if (ShouldShow(instance))
                {
                    seenIds.Add(instance.Id);
                    cardsToShow.Add(instance);
                }
            }
        }

        // 2. 战斗中所有牌堆里的实际卡牌（包含临时添加关键字的）
        var piles = new[] 
        { 
            combatState.Hand, 
            combatState.DrawPile, 
            combatState.DiscardPile, 
            combatState.ExhaustPile 
        };

        foreach (var pile in piles)
        {
            if (pile == null) continue;
            foreach (var card in pile.Cards)
            {
                if (card.HasModKeyword(CutesakiKeywords.Memory))
                {
                    if (ShouldShow(card))
                    {
                        seenIds.Add(card.Id);
                        cardsToShow.Add(card);
                    }
                }
            }
        }

        if (cardsToShow.Count == 0) return;

        var displayPile = new CardPile(PileType.Exhaust);
        foreach (var card in cardsToShow)
            displayPile.AddInternal(card);

        NCardPileScreen.ShowScreen(displayPile, Array.Empty<string>());
    }
}