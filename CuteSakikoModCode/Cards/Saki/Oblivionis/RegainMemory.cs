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

        // 手牌上限保护
        var hand = PileType.Hand.GetPile(Owner);
        int handSpace = 10 - (hand?.Cards.Count ?? 0);
        if (handSpace <= 0) return;

        int maxSelect = Math.Min(DynamicVars.Cards.IntValue, forgetPile.Cards.Count);
        maxSelect = Math.Min(maxSelect, handSpace);
        if (maxSelect <= 0) return;

        var prefs = new CardSelectorPrefs(
            new LocString("cards", "CUTE_SAKIKO_MOD_CARD_REGAIN_MEMORY.selectionScreenPrompt"),
            maxSelect,
            maxSelect
        );

        var candidates = forgetPile.Cards.ToList();
        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            candidates,
            Owner,
            prefs

        );

        // 快照，避免遍历时修改源集合
        var selectedList = selected.ToList();
        foreach (var card in selectedList)
        {
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom, this);
            card.SetToFreeThisCombat();
        }

        // 刷新遗忘堆 UI
        forgetPile.InvokeContentsChanged();
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}