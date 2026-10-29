using CuteSakikoMod.CuteSakikoModCode.Cards.Mod;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;

public class PigEat() : OtherModTokenCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal, CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Increase", 1)
    ];

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
        var read = DynamicVars["Increase"].IntValue;
        var totalIncrease = removeCount * read;
        if (totalIncrease > 0) await CreatureCmd.GainMaxHp(Owner.Creature, totalIncrease);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Increase"].UpgradeValueBy(1m);

    }
}