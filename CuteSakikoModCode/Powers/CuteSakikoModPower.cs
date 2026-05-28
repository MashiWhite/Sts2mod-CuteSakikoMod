using CuteSakikoMod.CuteSakikoModCode.Others;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Powers;

[RegisterPower(Inherit = true)]
public abstract class CuteSakikoModPower : ModPowerTemplate
{
    public override PowerAssetProfile AssetProfile => this.PowerAssetProfile();
}