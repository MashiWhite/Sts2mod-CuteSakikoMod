using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki;

[RegisterRelic(typeof(CuteSakiRelicPool), Inherit = true)]
public abstract class CuteSakiRelic : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => this.RelicAssetProfile();
}