using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Entities.Relics;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Mutsumi.Starter;

/// <summary>
///     怪话连篇（多言的欧洛巴斯精炼版）：
///     用阴阳怪气的怪话侧面伤人；每打出一张攻击牌获得 3 层言刃。
/// </summary>
public sealed class BarbedChatter : TalkativeRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override int WordBladePerAttack => 3;

    // 暂时复用多言的遗物图像，等专属图完成后再替换。
    public override RelicAssetProfile AssetProfile => this.RelicAssetProfile("TalkativeRelic");
}
