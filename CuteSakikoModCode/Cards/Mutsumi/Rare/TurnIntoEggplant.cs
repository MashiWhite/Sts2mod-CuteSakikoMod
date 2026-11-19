using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Archetypes;
using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Planting;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Rare;

/// <summary>
///     变身茄子（通用金卡）：2c 技能，选择除此卡外的 1 张手牌，
///     把它的当前状态复制进「茄子」并种下；升级后种下茄子+（基础重放次数 +1）。
/// </summary>
public class TurnIntoEggplant() : CuteMutsumiCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self), IMutsumiArchetypeCard
{
    public MutsumiArchetype Archetype => MutsumiArchetype.Cucumber;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override bool IsPlayable
    {
        get
        {
            if (Owner == null) return false;
            if (!PlantingCardPile.CanPlant(Owner, 1)) return false;

            var hand = PileType.Hand.GetPile(Owner);
            return hand != null && hand.Cards.Any(c => c is not TurnIntoEggplant);
        }
    }

    /// <summary>悬浮提示里展示会种下的茄子（茄苗阶段）。</summary>
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (IsCanonical || Owner == null) yield break;

            Eggplant? preview = null;
            try
            {
                preview = Owner.RunState.CreateCard<Eggplant>(Owner);
            }
            catch
            {
                preview = null;
            }

            if (preview == null) yield break;
            yield return HoverTipFactory.FromCard(preview);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hand = PileType.Hand.GetPile(Owner);
        if (hand == null) return;

        var candidates = hand.Cards.Where(c => c is not TurnIntoEggplant).ToList();
        if (candidates.Count == 0) return;

        CardModel? chosen;
        if (candidates.Count == 1)
        {
            chosen = candidates[0];
        }
        else
        {
            var prompt = new LocString("cards",
                "CUTE_SAKIKO_MOD_CARD_TURN_INTO_EGGPLANT.selectionScreenPrompt");
            var prefs = new CardSelectorPrefs(prompt, 1);
            var selected = await CardSelectCmd.FromHand(
                choiceContext, Owner, prefs, c => c is not TurnIntoEggplant, this);
            chosen = selected.FirstOrDefault();
        }

        if (chosen == null || !PlantingCardPile.CanPlant(Owner, 1)) return;

        // 复制当前状态（升级、附魔、关键词、临时数值）；原牌保留在手里。
        var snapshot = chosen.CreateClone();

        var eggplant = CombatState.CreateCard<Eggplant>(Owner);
        eggplant.SetSourceCard(snapshot);
        if (IsUpgraded)
            CardCmd.Upgrade(eggplant);
        eggplant.PlantedTurn = Owner.PlayerCombatState?.TurnNumber ?? 0;

        await CardPileCmd.AddGeneratedCardToCombat(
            eggplant, PlantingCardPile.GetPileType(), Owner);
    }
}
