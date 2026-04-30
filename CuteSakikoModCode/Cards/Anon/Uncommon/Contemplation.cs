using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class Contemplation() : CuteAnonCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var exhaustPile = PileType.Exhaust.GetPile(Owner);
        if (exhaustPile == null || exhaustPile.Cards.Count == 0) return;

        var availableCards = exhaustPile.Cards.Where(c => c.Id != Id).ToList();
        if (availableCards.Count == 0) return;

        var pickCount = IsUpgraded ? 2 : 1;
        var maxCount = availableCards.Count;
        if (maxCount < pickCount) pickCount = maxCount;

        // 根据数量选择不同的本地化键
        var promptKey = pickCount == 2
            ? "CUTE_SAKIKO_MOD_CARD_CONTEMPLATION.selectionScreenPrompt.2"
            : "CUTE_SAKIKO_MOD_CARD_CONTEMPLATION.selectionScreenPrompt.1";

        var prefs = new CardSelectorPrefs(
                new LocString("cards", promptKey),
                pickCount,
                pickCount
            )
            { RequireManualConfirmation = true };

        var selectedCards = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            availableCards,
            Owner,
            prefs
        );

        foreach (var card in selectedCards)
        {
            await CardPileCmd.Add(card, PileType.Hand, CardPilePosition.Bottom, this);
            card.SetToFreeThisTurn();
        }
    }

    protected override void OnUpgrade()
    {
    }
}