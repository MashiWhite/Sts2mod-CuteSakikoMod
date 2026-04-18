using BaseLib.Abstracts;
using BaseLib.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class GoldPower : CustomPowerModel
{
    public override string CustomPackedIconPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").PowerImagePath();

    public override string CustomBigIconPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").PowerImagePath();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;
}