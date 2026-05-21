using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki;

[RegisterCard(typeof(TokenCardPool), Inherit = true)]
public abstract class SakiMemoryCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    ModCardTemplate(cost, type, rarity, target)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Memory.GetModKeywordCardKeyword()];

    public override CardAssetProfile AssetProfile => this.CardAssetProfile();
}