using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Models.PotionPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Potions.Shared;

[RegisterPotion(typeof(SharedPotionPool), Inherit = true)]
public abstract class CuteSakikoSharedPotion : ModPotionTemplate
{
    public override PotionAssetProfile AssetProfile => this.PotionAssetProfile();
}