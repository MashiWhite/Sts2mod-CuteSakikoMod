
using MegaCrit.Sts2.Core.Entities.Powers;


namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff
{
    public sealed class PlayImmediatelyPower : CuteSakikoModPower
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.None; // 不可叠层
        
    }
}