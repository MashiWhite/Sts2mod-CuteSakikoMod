using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class AtkByMemory() : CuteSakikoModCard(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override bool GainsBlock => true;
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new BlockVar(8m, ValueProp.Move)
    ];

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
        // 获得格挡
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);

        // 用回忆卡牌填满手牌
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

        // 使用规范模板
        var canonicalCards = MemoryCardPile.GetCanonicalCards(Owner);
        if (canonicalCards.Count == 0) return;

        var newCards = new List<CardModel>();
        for (var i = 0; i < needed; i++)
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
        // 升级：格挡 8 -> 15
        DynamicVars.Block.UpgradeValueBy(7m);
    }
}