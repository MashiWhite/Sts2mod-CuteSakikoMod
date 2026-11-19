using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

[RegisterPower]
public class ChordTempStrengthPower : CuteSakikoTemporaryPower
{
    public override PowerAssetProfile AssetProfile =>
        new(TempStatIcons.Strength, TempStatIcons.Strength);

    public override PowerModel InternallyAppliedPower =>
        ModelDb.Power<StrengthPower>().ToMutable();

    protected override bool IsPositive => true;
    protected override bool UntilEndOfOtherSideTurn => false;
    protected override int LastForXExtraTurns => 0;
}