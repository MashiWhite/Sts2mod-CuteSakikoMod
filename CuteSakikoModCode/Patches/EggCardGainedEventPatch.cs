using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

public class EggCardGainedEventPatch
{
    // 精确指定要修补的方法签名，避免歧义
    [HarmonyPatch(typeof(CardPileCmd), nameof(CardPileCmd.Add), typeof(CardModel), typeof(PileType),
        typeof(CardPilePosition), typeof(AbstractModel), typeof(bool))]
    public static class CardAddPatch
    {
        public static void Postfix(CardModel card, PileType newPileType, CardPilePosition position,
            AbstractModel source, bool skipVisuals)
        {
            // 使用 newPileType 而不是 newPile
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