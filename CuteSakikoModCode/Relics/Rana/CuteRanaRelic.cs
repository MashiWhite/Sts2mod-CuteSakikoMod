using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools.Rana;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Rana;

[RegisterRelic(typeof(CuteRanaRelicPool), Inherit = true)]
public abstract class CuteRanaRelic : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => this.RelicAssetProfile();
}