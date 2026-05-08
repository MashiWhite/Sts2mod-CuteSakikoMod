using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Models.RelicPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Relics;

[RegisterRelic(typeof(EventRelicPool), Inherit = true)]
public abstract class CuteSakikoModRelic : ModRelicTemplate
{
    public override RelicAssetProfile AssetProfile => this.RelicAssetProfile();
}