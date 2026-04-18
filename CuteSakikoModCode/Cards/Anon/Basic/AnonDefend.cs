
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Pools.Anon;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Commands;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Basic;

[Pool(typeof(CuteAnonCardPool))]
public class AnonDefend : CuteAnonCard
{
    public AnonDefend() : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    protected override HashSet<CardTag> CanonicalTags => new HashSet<CardTag> { CardTag.Defend };

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(6m, ValueProp.Move)
    ];
    
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}