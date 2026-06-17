using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class ManyNeko : CuteRanaCard
{
    public ManyNeko() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Neko.GetModCardKeyword());
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var allNekoCards = ModelDb.CardPool<CuteSakikoTokenCardPool>()
            .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
            .Where(c => c.Keywords.Contains(CutesakiKeywords.Neko.GetModCardKeyword()))
            .ToList();
        if (allNekoCards.Count == 0) return;

        var combatState = Owner.Creature.CombatState!;
        var rng = Owner.RunState.Rng.CombatCardGeneration;

        // 抽牌堆
        var templateDraw = rng.NextItem(allNekoCards);
        var cardDraw = combatState.CreateCard(templateDraw, Owner);
        if (IsUpgraded)
        {
            cardDraw.UpgradeInternal();
            cardDraw.FinalizeUpgradeInternal();
        }
        var drawResult = await CardPileCmd.AddGeneratedCardToCombat(cardDraw, PileType.Draw, Owner);

        // 手牌堆
        var templateHand = rng.NextItem(allNekoCards);
        var cardHand = combatState.CreateCard(templateHand, Owner);
        if (IsUpgraded)
        {
            cardHand.UpgradeInternal();
            cardHand.FinalizeUpgradeInternal();
        }
        await CardPileCmd.AddGeneratedCardToCombat(cardHand, PileType.Hand, Owner);

        // 弃牌堆
        var templateDiscard = rng.NextItem(allNekoCards);
        var cardDiscard = combatState.CreateCard(templateDiscard, Owner);
        if (IsUpgraded)
        {
            cardDiscard.UpgradeInternal();
            cardDiscard.FinalizeUpgradeInternal();
        }
        var discardResult = await CardPileCmd.AddGeneratedCardToCombat(cardDiscard, PileType.Discard, Owner);

        // 预览抽牌堆和弃牌堆的添加，刷新 UI 数字
        CardCmd.PreviewCardPileAdd(new List<CardPileAddResult> { drawResult, discardResult });
    }

    protected override void OnUpgrade()
    {
     EnergyCost.UpgradeBy(-1);
    }
}