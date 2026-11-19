using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Archetypes;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Common;

/// <summary>
///     真是低低在下（小睦/Cucumber 白卡）：1c 技能，获得 11 点格挡，
///     并获得 2 层缩小；升级后 12 点格挡、1 层缩小。
/// </summary>
public class SoLowly()
    : CuteMutsumiCard(1, CardType.Skill, CardRarity.Common, TargetType.Self), IMutsumiArchetypeCard
{
    public MutsumiArchetype Archetype => MutsumiArchetype.Cucumber;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(11m, ValueProp.Move),
        new PowerVar<ShrinkPower>(2m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips =>
        [HoverTipFactory.FromPower<ShrinkPower>()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<ShrinkPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["ShrinkPower"].IntValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(1m);
        DynamicVars["ShrinkPower"].UpgradeValueBy(-1m);
    }
}
