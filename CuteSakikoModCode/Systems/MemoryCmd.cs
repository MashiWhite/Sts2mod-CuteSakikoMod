using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Systems
{
    public static class MemoryCmd
    {
        public static void Forget(PlayerChoiceContext choiceContext, IEnumerable<CardModel> cards, CardModel? source = null, bool removeFromMemory = true)
        {
            var list = cards.ToList();
            if (list.Count == 0) return;
            var player = list[0].Owner;

            CardPile? forgetPile = ForgetCardPile.Get(player);
            bool useFallback = false;
            if (forgetPile == null)
            {
                forgetPile = PileType.Exhaust.GetPile(player);
                if (forgetPile == null) return;
                useFallback = true;
            }

            var memoryPile = removeFromMemory ? MemoryCardPile.Get(player) : null;
            int forgottenCount = 0;

            foreach (var card in list)
            {
                if (card.Pile?.Type == forgetPile.Type) continue;
                card.Pile?.RemoveInternal(card, silent: true);
                forgetPile.AddInternal(card, silent: true);
                forgottenCount++;

                if (!useFallback && removeFromMemory && memoryPile != null)
                {
                    var toRemove = memoryPile.Cards.Where(c => c.Id == card.Id).ToList();
                    foreach (var mCard in toRemove)
                        memoryPile.RemoveInternal(mCard, silent: true);
                }
            }

            if (forgottenCount > 0)
            {
                forgetPile.InvokeContentsChanged();

                if (!useFallback)
                {
                    var pressure = player.Creature.GetPower<PressurePower>();
                    if (pressure != null)
                        pressure.SetAmount(pressure.Amount - forgottenCount * 2);
                }
                
                _ = MemoryCardPileManager.FireOnForgottenCards(choiceContext, list, source);
            }
        }

        public static List<CardModel> Recall(PlayerChoiceContext choiceContext, Player player, int count, bool upgraded, CardModel source = null)
        {
            var memoryPile = MemoryCardPile.Get(player);
            if (memoryPile == null || memoryPile.Cards.Count == 0)
                return new List<CardModel>();
            var rng = player.RunState.Rng.Shuffle;
            var available = memoryPile.Cards.ToList();
            if (count > available.Count) count = available.Count;
            var chosen = new List<CardModel>();
            var tempList = available.ToList();
            for (int i = 0; i < count; i++)
            {
                int index = rng.NextInt(0, tempList.Count);
                chosen.Add(tempList[index]);
                tempList.RemoveAt(index);
            }
            var newCards = new List<CardModel>();
            foreach (var template in chosen)
            {
                var clone = player.RunState.CloneCard(template);
                if (upgraded && !clone.IsUpgraded)
                    clone.UpgradeInternal();
                newCards.Add(clone);
            }
            if (newCards.Count > 0)
            {
                var handPile = player.PlayerCombatState?.Hand;
                if (handPile != null)
                {
                    foreach (var c in newCards)
                        handPile.AddInternal(c, silent: true);
                    handPile.InvokeContentsChanged();
                }
            }
            return newCards;
        }
    }
}