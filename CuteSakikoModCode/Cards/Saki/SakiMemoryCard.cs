using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki;

[Pool(typeof(TokenCardPool))]
public abstract class SakiMemoryCard(int cost, CardType type, CardRarity rarity, TargetType target) :
    CustomCardModel(cost, type, rarity, target)
{
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Memory];
    public virtual Task ProcessMemoryEffect(PlayerChoiceContext choiceContext)
    {
        return Task.CompletedTask;
    }
    

    public override string CustomPortraitPath => 
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();
    public override string BetaPortraitPath => 
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();
}