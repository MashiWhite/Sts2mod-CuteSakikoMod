using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

// 添加 LINQ 支持

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class AllForget() : CuteSakikoModCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var handCards = PileType.Hand.GetPile(Owner)?.Cards.ToList();
        if (handCards == null || handCards.Count == 0) return;

        var cardCount = handCards.Count;

        foreach (var card in handCards)
            await CardCmd.Exhaust(choiceContext, card);

        await CardPileCmd.Draw(choiceContext, cardCount, Owner);

        var pressure = Owner.Creature.GetPower<PressurePower>();
        if (pressure != null)
        {
            var reducePerCard = IsUpgraded ? 2 : 1;
            var totalReduce = cardCount * reducePerCard;
            // 使用 PowerCmd.ModifyAmount 安全减少压力层数
            await PowerCmd.ModifyAmount(choiceContext, pressure, -totalReduce, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级效果已在逻辑中处理
    }
}