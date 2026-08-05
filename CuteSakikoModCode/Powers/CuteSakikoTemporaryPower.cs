using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Powers;

/// <summary>
/// 拥有统一图标分辨逻辑的临时力量基类。
/// 继承自 RitsuLib 的 ModTemporaryPowerTemplate，同时使用 CuteSakikoMod 的 PowerAssetProfile 扩展。
/// </summary>
[RegisterPower(Inherit = true)]
public abstract class CuteSakikoTemporaryPower : ModTemporaryPowerTemplate
{
    public override LocString Title => new("powers", Id.Entry + ".title");
    public override PowerAssetProfile AssetProfile => this.PowerAssetProfile();
    public override AbstractModel OriginModel => null;
}