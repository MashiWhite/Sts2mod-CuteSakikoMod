
using MegaCrit.Sts2.Core.Entities.Powers;


namespace CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;

public sealed class AtkByMemoryPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single; 
    public override bool AllowNegative => false;
}