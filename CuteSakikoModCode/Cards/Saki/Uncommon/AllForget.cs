using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Keywords;

// 添加 LINQ 支持

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class AllForget() : CuteSakikoModCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Sakiforget);
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Memory);
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
            MemoryCmd.Forget(choiceContext, handCards, this);

        await CardPileCmd.Draw(choiceContext, cardCount, Owner);
        
    }

    protected override void OnUpgrade()
    {
        // 升级效果已在逻辑中处理
    }
}