using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;

[Pool(typeof(TokenCardPool))]
public class PigEat : CustomCardModel
{
    public PigEat() : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Ethereal };

    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var handPile = PileType.Hand.GetPile(Owner);
        if (handPile == null) return;

        // 获取当前手牌中的所有卡牌（包括自身）
        var cardsInHand = handPile.Cards.ToList();
        var removeCount = cardsInHand.Count;
        if (removeCount == 0) return;

        // 移除所有手牌
        foreach (var card in cardsInHand) await CardPileCmd.RemoveFromCombat(card);

        // 每移除一张增加最大生命值（基础1，升级2）
        var increasePerCard = IsUpgraded ? 2 : 1;
        var totalIncrease = removeCount * increasePerCard;
        if (totalIncrease > 0) await CreatureCmd.GainMaxHp(Owner.Creature, totalIncrease);
    }

    protected override void OnUpgrade()
    {
        // 升级效果在 OnPlay 中通过 IsUpgraded 处理
    }
}