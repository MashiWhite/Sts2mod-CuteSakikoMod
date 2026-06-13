using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class ReplyHelper() : CuteRanaCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyAlly)
{
    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;
        var targetPlayer = cardPlay.Target.Player;
        if (targetPlayer == null || targetPlayer == Owner) return;

        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 1),
            c => c != this,
            this
        );
        var selected = selectedCards.FirstOrDefault();
        if (selected == null) return;

        var canonicalCard = ModelDb.GetById<CardModel>(selected.Id);
        var wasUpgraded = selected.IsUpgraded;

        await CardPileCmd.RemoveFromCombat(selected);

        var newCard = CombatState.CreateCard(canonicalCard, targetPlayer);
        if (wasUpgraded)
        {
            newCard.UpgradeInternal();
            newCard.FinalizeUpgradeInternal();
        }
        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, targetPlayer);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}