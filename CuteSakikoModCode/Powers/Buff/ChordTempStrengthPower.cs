using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

[RegisterPower]
public class ChordTempStrengthPower : CuteSakikoTemporaryPower
{
    public override PowerModel InternallyAppliedPower =>
        ModelDb.Power<StrengthPower>().ToMutable();

    protected override bool IsPositive => true;
    protected override bool UntilEndOfOtherSideTurn => false;
    protected override int LastForXExtraTurns => 0;
}