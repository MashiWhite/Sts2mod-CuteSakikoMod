using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Archetypes;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Uncommon;

/// <summary>
///     难逃若叶（小睦/Cucumber 蓝卡）：1c 技能。
///     获得 2 层虚弱，然后抽 5 张牌；升级后获得 1 层虚弱。
/// </summary>
public class NoEscapeFromWakaba()
    : CuteMutsumiCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self), IMutsumiArchetypeCard
{
    public MutsumiArchetype Archetype => MutsumiArchetype.Cucumber;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<WeakPower>(2m),
        new CardsVar(5)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<WeakPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["WeakPower"].IntValue,
            Owner.Creature,
            this);

        await CardPileCmd.Draw(
            choiceContext,
            DynamicVars.Cards.IntValue,
            Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["WeakPower"].UpgradeValueBy(-1m);
    }
}
