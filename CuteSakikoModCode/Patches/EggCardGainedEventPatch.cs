using CuteSakikoMod.CuteSakikoModCode.Cards;
using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs;
using CuteSakikoMod.CuteSakikoModCode.Others;
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
            AbstractModel clonedBy, bool skipVisuals)
        {
            if (newPileType == PileType.Deck && IsEggCard(card))
                EggCardGainedEvent.Trigger(card);
        }

        private static bool IsEggCard(CardModel card)
        {
            // 判断卡牌是否继承自 CuteSakikoModEggCard
            return card is CuteSakikoModEggCard;
        }
    }
}