using BaseLib.Abstracts;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools.Anon;
using StringExtensions = BaseLib.Extensions.StringExtensions;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon;

[Pool(typeof(CuteAnonRelicPool))]
public abstract class CuteAnonRelic : CustomRelicModel
{
    
    // 大图标路径（用于遗物详情等）
    protected override string BigIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").BigRelicImagePath();

    // 普通图标路径（背包、状态栏小图标）
    public override string PackedIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").RelicImagePath();

    protected override string PackedIconOutlinePath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + "_outline.png").RelicImagePath();
    
}