using MegaCrit.Sts2.Core.Entities.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;


public sealed class SweepPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single; // 只需要存在，不叠层
}