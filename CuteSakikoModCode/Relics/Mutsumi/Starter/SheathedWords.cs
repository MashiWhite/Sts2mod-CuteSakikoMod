using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Mutsumi.Starter;

/// <summary>
///     缄口藏锋（缄默的欧洛巴斯精炼版）：
///     怕自己的话伤人而闭嘴藏锋；未打出攻击牌时获得 6 层言刃。
/// </summary>
public sealed class SheathedWords : SilenceRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override int WordBladeOnSilence => 6;

    // 暂时复用缄默的遗物图像，等专属图完成后再替换。
    public override RelicAssetProfile AssetProfile => this.RelicAssetProfile("SilenceRelic");
}
