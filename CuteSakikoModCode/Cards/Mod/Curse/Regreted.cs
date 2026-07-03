using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Curse;

public class Regreted : ModCurseCard
{
    public Regreted() : base(0, CardType.Curse, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override bool IsPlayable => true;
    public override int MaxUpgradeLevel => 0;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new HpLossVar(2m); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, 1, Owner);
        VfxCmd.PlayOnCreatureCenter(Owner.Creature, "vfx/vfx_bloody_impact");
        await CreatureCmd.Damage(choiceContext, Owner.Creature,
            new DamageVar(DynamicVars.HpLoss.BaseValue, ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move),
            this, null);
    }
}