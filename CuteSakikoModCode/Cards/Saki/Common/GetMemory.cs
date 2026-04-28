
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common
{
    public class GetMemory() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                yield return HoverTipFactory.FromPower<PressurePower>();
                yield return HoverTipFactory.FromPower<BreakDownPower>();
                yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory);
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            try
            {
                await PowerCmd.Apply<PressurePower>(choiceContext, Owner.Creature, 3, Owner.Creature, this);

                var exhaustedMemoryIds = SakiMemoryManager.ExhaustedMemoryIds.ToHashSet();

                var availableMemoryCards = ModelDb.AllCards
                    .Where(card =>
                        card.CanonicalKeywords.Contains(CutesakiKeywords.Memory) && !exhaustedMemoryIds.Contains(card.Id))
                    .ToList();

                if (availableMemoryCards.Count > 0)
                {
                    var randomCard = CardFactory.GetDistinctForCombat(
                        Owner,
                        availableMemoryCards,
                        1,
                        Owner.RunState.Rng.CombatCardGeneration
                    ).FirstOrDefault();

                    if (randomCard != null)
                    {
                        CardModel newCard = randomCard.IsMutable ? randomCard : randomCard.ToMutable();

                        if (IsUpgraded)
                        {
                            newCard.UpgradeInternal();
                            newCard.FinalizeUpgradeInternal();
                        }

                        await CardPileCmd.AddGeneratedCardToCombat(newCard, PileType.Hand, Owner);
                    }
                }
            }
            finally
            {
                await CardCmd.Discard(choiceContext, this);
            }
        }

        protected override void OnUpgrade()
        {
        }
    }
}