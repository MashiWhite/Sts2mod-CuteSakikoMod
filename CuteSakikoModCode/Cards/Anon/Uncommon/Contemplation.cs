
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
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
            if (exhaustPile == null || exhaustPile.Cards.Count == 0)
                return;

            // 排除所有其他“沉思”牌（同一ID）
            var availableCards = exhaustPile.Cards.Where(c => c.Id != this.Id).ToList();
            if (availableCards.Count == 0)
                return;

            int pickCount = IsUpgraded ? 2 : 1;
            int maxCount = availableCards.Count;
            if (maxCount < pickCount) pickCount = maxCount;

            var prefs = new CardSelectorPrefs(
                new LocString("cards", "CUTESAKIKOMOD-CONTEMPLATION.selectionScreenPrompt"),
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
            // 升级效果在 OnPlay 中通过 IsUpgraded 控制数量
        }
    }
}