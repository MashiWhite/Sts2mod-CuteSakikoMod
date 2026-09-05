using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

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

        var selected = await CardSelectCmd.FromCombatPile(
            choiceContext,
            forgetPile,
            Owner,
            prefs,
            _ => true
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