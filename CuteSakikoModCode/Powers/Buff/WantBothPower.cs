using MegaCrit.Sts2.Core.Entities.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public class WantBothPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 改为可叠层
    public override bool AllowNegative => false;
}