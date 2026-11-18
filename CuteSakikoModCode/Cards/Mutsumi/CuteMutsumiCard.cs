using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools.Mutsumi;
using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi;

[RegisterCard(typeof(CuteMutsumiCardPool), Inherit = true)]
public abstract class CuteMutsumiCard(int cost, CardType type, CardRarity rarity, TargetType target)
    : ModCardTemplate(cost, type, rarity, target)
{
    public override CardAssetProfile AssetProfile => this.CardAssetProfile();
}
