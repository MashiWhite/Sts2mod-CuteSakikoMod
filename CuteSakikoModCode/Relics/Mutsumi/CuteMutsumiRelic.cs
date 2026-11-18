using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools.Mutsumi;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Mutsumi;

[RegisterRelic(typeof(CuteMutsumiRelicPool), Inherit = true)]
public abstract class CuteMutsumiRelic : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => this.RelicAssetProfile();
}
