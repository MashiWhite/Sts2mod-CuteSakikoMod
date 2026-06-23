using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

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
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory.GetModCardKeyword());
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sakiforget.GetModCardKeyword());
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

        var memorySnapshots = MemoryCardPile.GetCanonicalCards(Owner);
        if (memorySnapshots.Count == 0) return;

        var rng = Owner.RunState.Rng.CombatCardGeneration;
        var newCards = new List<CardModel>();

        // 每次独立随机抽取一张快照，可以重复抽到同一张
        for (int i = 0; i < needed; i++)
        {
            int randomIndex = rng.NextInt(memorySnapshots.Count);
            var snapshot = memorySnapshots[randomIndex];
            var newCard = MemoryCardPile.CreateCardFromMemorySnapshot(Owner, snapshot);
            if (newCard != null)
                newCards.Add(newCard);
        }

        if (newCards.Count > 0)
            await CardPileCmd.AddGeneratedCardsToCombat(newCards, PileType.Hand, Owner);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}