using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Archetypes;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Planting;
using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Uncommon;

/// <summary>
///     赠君心头好（小睦/Cucumber 多人专属蓝卡）：1c 技能。
///     给予目标队友 2 层言刃，并为该队友种植 1 张黄瓜；
///     升级后 3 层言刃，种下的黄瓜为黄瓜+。
/// </summary>
public class GiftOfMyHeart()
    : CuteMutsumiCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly), IMutsumiArchetypeCard
{
    public MutsumiArchetype Archetype => MutsumiArchetype.Cucumber;

    public override CardMultiplayerConstraint MultiplayerConstraint =>
        CardMultiplayerConstraint.MultiplayerOnly;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
        [new PowerVar<WordBladePower>(2m)];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
    [
        HoverTipFactory.FromPower<WordBladePower>(),
        HoverTipFactory.FromCard<Cucumber>(IsUpgraded)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target?.Player == null) return;

        var ally = cardPlay.Target.Player;

        await PowerCmd.Apply<WordBladePower>(
            choiceContext,
            cardPlay.Target,
            DynamicVars["WordBladePower"].IntValue,
            cardPlay.Target,
            this);

        if (!PlantingCardPile.CanPlant(ally, 1)) return;

        var cucumber = CombatState.CreateCard<Cucumber>(ally);
        if (IsUpgraded)
            CardCmd.Upgrade(cucumber);

        cucumber.PlantedTurn = ally.PlayerCombatState?.TurnNumber ?? 0;

        await CardPileCmd.AddGeneratedCardToCombat(
            cucumber,
            PlantingCardPile.GetPileType(),
            ally);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["WordBladePower"].UpgradeValueBy(1m);
    }
}
