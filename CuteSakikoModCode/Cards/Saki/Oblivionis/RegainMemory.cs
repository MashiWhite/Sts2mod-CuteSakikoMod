using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Oblivionis;

public class RegainMemory : CuteObCard
{
    public RegainMemory() : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }
    
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new CardsVar(1); }
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var forgetPile = ForgetCardPile.Get(Owner);
        if (forgetPile == null || forgetPile.Cards.Count == 0) return;

        int maxSelect = DynamicVars.Cards.IntValue;
        maxSelect = Math.Min(maxSelect, forgetPile.Cards.Count);
        if (maxSelect <= 0) return;

        var prefs = new CardSelectorPrefs(
            new LocString("cards", "CUTE_SAKIKO_MOD_CARD_REGAIN_MEMORY.selectionScreenPrompt"),
            maxSelect,
            maxSelect
        );

        // 改用 FromSimpleGrid 取代 FromCombatPile：遗忘堆是自定义牌堆，
        // FromCombatPile 的牌堆类型校验路径会被"选牌抓取"类联机补丁
        // （如 Multiplayer Desync Fix，工坊 3792872414）改写，导致非出牌方抛
        // InvalidOperationException: Cannot perform on a non combat pile → 两端分叉卡死。
        // FromSimpleGrid 只接收显式牌列表，不做牌堆类型校验
        // （同 mod 的 MemoryCmd.Recall / OnlyOblivion 即用此法，联机从未出问题）。
        var candidates = forgetPile.Cards.ToList();
        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            prefs
        );

        foreach (var card in selected)
        {
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom, this);
            card.SetToFreeThisTurn();
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}