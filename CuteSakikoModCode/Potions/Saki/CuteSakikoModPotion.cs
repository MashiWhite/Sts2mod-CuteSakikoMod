using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools.Ob;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Potions.Saki;

[RegisterPotion(typeof(CuteSakiPotionPool), Inherit = true)]
public abstract class CuteSakikoModPotion : ModPotionTemplate
{
    public override PotionAssetProfile AssetProfile => this.PotionAssetProfile();
}