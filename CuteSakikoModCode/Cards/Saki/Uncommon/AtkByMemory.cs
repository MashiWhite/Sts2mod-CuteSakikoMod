using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
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

        // 用回忆卡牌填满手牌（原有逻辑保持不变）
        await FillHandWithMemoryCards();
    }

    private async Task FillHandWithMemoryCards()
    {
        var handPile = PileType.Hand.GetPile(Owner);
        if (handPile == null) return;

        var maxHandSize = 10;
        var currentSize = handPile.Cards.Count;
        var needed = maxHandSize - currentSize;
        if (needed <= 0) return;

        var canonicalCards = MemoryCardPile.GetCanonicalCards(Owner);
        if (canonicalCards.Count == 0) return;

        // 打乱快照列表（使用 RNG）
        var shuffled = canonicalCards.OrderBy(_ => Owner.RunState.Rng.Shuffle.NextFloat()).ToList();
    
        var newCards = new List<CardModel>();
        var usedIds = new HashSet<ModelId>();
        for (var i = 0; i < Math.Min(needed, shuffled.Count); i++)
        {
            var template = shuffled[i];
            if (usedIds.Contains(template.Id)) continue;
            usedIds.Add(template.Id);
        
            var newCard = MemoryCardPile.CreateCardFromMemorySnapshot(Owner, template);
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