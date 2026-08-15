using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools.Ob;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki;

[RegisterRelic(typeof(CuteObRelicPool), Inherit = true)]
public abstract class CuteObRelic : CuteSakiRelic
{
    public override RelicAssetProfile AssetProfile => this.RelicAssetProfile();
}