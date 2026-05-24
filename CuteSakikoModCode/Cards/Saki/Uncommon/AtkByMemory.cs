using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Factories;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class AtkByMemory : CuteSakikoModCard
{
    public AtkByMemory() : base(3, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Sakiforget);
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Memory);
            yield return HoverTipFactory.FromPower<AtkByMemoryPower>();
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AtkByMemoryPower>(
            choiceContext,
            Owner.Creature,
            1,
            Owner.Creature,
            cardPlay.Card
        );

        // 用回忆卡牌填满手牌（原有逻辑保持不变）
        await FillHandWithMemoryCards();
    }

    private async Task FillHandWithMemoryCards()
    {
        var handPile = PileType.Hand.GetPile(Owner);
        if (handPile == null) return;

        int maxHandSize = 10;
        int currentSize = handPile.Cards.Count;
        int needed = maxHandSize - currentSize;
        if (needed <= 0) return;

        var canonicalCards = MemoryCardPile.GetCanonicalCards(Owner);
        if (canonicalCards.Count == 0) return;

        var newCards = new List<CardModel>();
        for (int i = 0; i < needed; i++)
        {
            var newCard = CardFactory.GetDistinctForCombat(
                Owner,
                canonicalCards,
                1,
                Owner.RunState.Rng.CombatCardGeneration
            ).FirstOrDefault();
            if (newCard != null) newCards.Add(newCard);
        }

        if (newCards.Count > 0)
            await CardPileCmd.AddGeneratedCardsToCombat(newCards, PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}