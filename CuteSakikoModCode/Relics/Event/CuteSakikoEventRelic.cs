using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event;

[RegisterRelic(typeof(EventRelicPool), Inherit = true)]
public abstract class CuteSakikoEventRelic : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => this.RelicAssetProfile();
}