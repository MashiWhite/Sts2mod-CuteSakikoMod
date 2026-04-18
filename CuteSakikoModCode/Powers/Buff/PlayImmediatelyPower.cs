using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using StringExtensions = BaseLib.Extensions.StringExtensions;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff
{
    public sealed class PlayImmediatelyPower : CustomPowerModel
    {
        public override string CustomPackedIconPath =>
            (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").PowerImagePath();

        public override string CustomBigIconPath =>
            (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").PowerImagePath();

        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.None; // 不可叠层
        
    }
}