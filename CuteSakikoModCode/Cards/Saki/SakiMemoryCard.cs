using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki;

[RegisterCard(typeof(TokenCardPool), Inherit = true)]
public abstract class SakiMemoryCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    ModCardTemplate(cost, type, rarity, target)
{
    protected override IEnumerable<string> RegisteredKeywordIds => [CutesakiKeywords.Memory,CutesakiKeywords.Sakiforget];
    
    public override CardAssetProfile AssetProfile => this.CardAssetProfile();
    
}