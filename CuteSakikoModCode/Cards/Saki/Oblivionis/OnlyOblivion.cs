using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Oblivionis;

public class OnlyOblivion : CuteObCard
{
    public OnlyOblivion() : base(1, CardType.Skill, CardRarity.Ancient, TargetType.None)
    {
    }
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sakiforget.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(1)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cards = DynamicVars.Cards.IntValue;
        // 收集所有目标牌堆中的卡牌
        var allCards = new List<CardModel>();

        // 手牌
        var hand = PileType.Hand.GetPile(Owner);
        if (hand != null) allCards.AddRange(hand.Cards);

        // 抽牌堆
        var draw = PileType.Draw.GetPile(Owner);
        if (draw != null) allCards.AddRange(draw.Cards);

        // 弃牌堆
        var discard = PileType.Discard.GetPile(Owner);
        if (discard != null) allCards.AddRange(discard.Cards);

        // 消耗堆
        var exhaust = PileType.Exhaust.GetPile(Owner);
        if (exhaust != null) allCards.AddRange(exhaust.Cards);

        // 遗忘堆（ModCardPile）
        var forgetPile = ForgetCardPile.Get(Owner);
        if (forgetPile != null) allCards.AddRange(forgetPile.Cards);

        if (allCards.Count == 0) return;

        var prefs = new CardSelectorPrefs(
            new LocString("cards", "CUTE_SAKIKO_MOD_CARD_ONLY_OBLIVION.selectionScreenPrompt"),
            cards,
            cards
        );

        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            allCards,
            Owner,
            prefs
        );

        var cardToForget = selected.FirstOrDefault();
        if (cardToForget == null) return;

        // 如果选中的牌在遗忘堆，则只需打出（已在遗忘堆，不再重复遗忘）
        bool alreadyForgotten = cardToForget.Pile == forgetPile;

        // 打出选中的牌
        await CardCmd.AutoPlay(choiceContext, cardToForget, null);

        // 遗忘（若原本不在遗忘堆）
        if (!alreadyForgotten)
        {
            await MemoryCmd.Forget(choiceContext, new[] { cardToForget }, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}