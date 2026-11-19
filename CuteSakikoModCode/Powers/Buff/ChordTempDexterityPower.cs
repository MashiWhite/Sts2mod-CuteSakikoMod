using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

[RegisterPower]
public class ChordTempDexterityPower : CuteSakikoTemporaryPower
{
    public override PowerAssetProfile AssetProfile =>
        new(TempStatIcons.Dexterity, TempStatIcons.Dexterity);

    public override PowerModel InternallyAppliedPower =>
        ModelDb.Power<DexterityPower>().ToMutable();

    protected override bool IsPositive => true;
    protected override bool UntilEndOfOtherSideTurn => false;
    protected override int LastForXExtraTurns => 0;
}