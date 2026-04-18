using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

// 添加 LINQ 支持

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

[Pool(typeof(CuteSakiCardPool))]
public class AllForget() : CustomCardModel(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    protected override IEnumerable<IHoverTip> ExtraHoverTips
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
            await PowerCmd.ModifyAmount(pressure, -totalReduce, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级效果已在逻辑中处理
    }
}