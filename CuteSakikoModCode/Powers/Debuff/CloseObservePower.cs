using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;

public class CloseObservePower : CuteSakikoModPower
{
    private const string DamageIncrease = "DamageIncrease";

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DynamicVar("DamageIncrease", 1.5m); }
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || !props.IsPoweredAttack())
            return 1m;

        var multiplier = DynamicVars["DamageIncrease"].BaseValue;
        if (dealer != null)
        {
            var relic = dealer.Player?.GetRelic<PaperPhrog>();
            if (relic != null)
                multiplier = relic.ModifyVulnerableMultiplier(target, multiplier, props, dealer, cardSource);
            var cruelty = dealer.GetPower<CrueltyPower>();
            if (cruelty != null)
                multiplier = cruelty.ModifyVulnerableMultiplier(target, multiplier, props, dealer, cardSource);
        }

        var debilitate = target.GetPower<DebilitatePower>();
        if (debilitate != null)
            multiplier = debilitate.ModifyVulnerableMultiplier(target, multiplier, props, dealer, cardSource);

        return multiplier;
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy) return;
        await PowerCmd.TickDownDuration(this);
    }
}