using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models;
using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Rooms;
using HarmonyLib;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons
{
    [RegisterSingleton]
    public sealed class MemoryCardPileManager : HookedSingletonModel
    {
        private static readonly CardKeyword MemoryKeyword = ModKeywordRegistry.GetCardKeyword(CutesakiKeywords.Memory);

        public MemoryCardPileManager() : base(receiveCombatHooks: true, receiveRunHooks: false) { }
        public override bool ShouldReceiveCombatHooks => true;

        // 改回异步事件，让 OblivionisPower 等能正常订阅
        public static event Func<PlayerChoiceContext, IReadOnlyList<CardModel>, CardModel?, Task>? OnForgottenCards;
        
        // 放在 OnForgottenCards 事件定义后面
        internal static Task FireOnForgottenCards(PlayerChoiceContext choiceContext, IReadOnlyList<CardModel> cards, CardModel? source)
        {
            return OnForgottenCards != null ? OnForgottenCards.Invoke(choiceContext, cards, source) : Task.CompletedTask;
        }

        [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
        private static class CardModel_OnPlayWrapper_Patch
        {
            public static void Postfix(CardModel __instance)
            {
                if (__instance.HasModKeyword(CutesakiKeywords.Memory))
                    __instance.EnergyCost.AddThisCombat(1);
            }
        }

        public override async Task AfterCombatEnd(CombatRoom room)
        {
            MemoryCardPile.Clear();
            ForgetCardPile.Clear();
            await Task.CompletedTask;
        }

        [HarmonyPatch(typeof(CombatManager), "DoTurnEnd")]
        public static class CombatManager_DoTurnEnd_Patch
        {
            public static void Postfix(Player player, PlayerChoiceContext choiceContext)
            {
                if (!CombatManager.Instance.IsInProgress) return;
                var hand = player.PlayerCombatState?.Hand;
                if (hand == null) return;
                var memoryCards = hand.Cards
                    .Where(c => c.HasModKeyword(CutesakiKeywords.Memory))
                    .ToList();
                if (memoryCards.Count > 0)
                    MemoryCmd.Forget(choiceContext, memoryCards, null, removeFromMemory: true);
            }
        }
    }
}