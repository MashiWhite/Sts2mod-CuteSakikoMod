using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

public class EggCardGainedEventPatch
{
    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(CardModel), typeof(PileType),
        typeof(CardPilePosition), typeof(AbstractModel), typeof(bool))]
    public static class CardAddPatch
    {
        public static void Postfix(CardModel card, PileType newPileType, CardPilePosition position,
            AbstractModel clonedBy, bool skipVisuals)   // ← 这里改名为 clonedBy
        {
            if (newPileType == PileType.Deck && IsEggCard(card))
                EggCardGainedEvent.Trigger(card);
        }

        private static bool IsEggCard(CardModel card)
        {
            var eggPool = ModelDb.CardPool<CuteSakikoEggCardPool>();
            return eggPool.AllCardIds.Contains(card.Id);
        }
    }
}