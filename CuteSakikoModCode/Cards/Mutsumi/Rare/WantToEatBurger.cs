using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Archetypes;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Status;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Rare;

/// <summary>
///     我要吃汉堡（莫提斯金卡）：0c 技能。
///     将 1 张汉堡加入手牌，抽 2 张牌，获得 1 点能量；升级后 2 点能量。
/// </summary>
public class WantToEatBurger()
    : CuteMutsumiCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self), IMutsumiArchetypeCard
{
    public MutsumiArchetype Archetype => MutsumiArchetype.Mortis;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2),
        new EnergyVar(1)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromCard<Hamburger>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var hamburger = CombatState.CreateCard<Hamburger>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(hamburger, PileType.Hand, Owner);

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.IntValue, Owner);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}
