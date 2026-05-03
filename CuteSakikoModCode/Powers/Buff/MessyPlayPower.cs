using MegaCrit.Sts2.Core.Entities.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class MessyPlayPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    // 不再重写 AfterCardPlayed，所有音符逻辑交由 AnonGuitar 统一处理
}