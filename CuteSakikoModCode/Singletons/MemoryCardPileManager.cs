
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models;
using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Rooms;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Runs;


namespace CuteSakikoMod.CuteSakikoModCode.Singletons
{
    [RegisterSingleton]
    public sealed class MemoryCardPileManager : HookedSingletonModel
    {
        private static readonly CardKeyword MemoryKeyword = ModKeywordRegistry.GetCardKeyword(CutesakiKeywords.Memory);

        public MemoryCardPileManager() : base(receiveCombatHooks: true, receiveRunHooks: false) { }
        public override bool ShouldReceiveCombatHooks => true;

        public static event Func<PlayerChoiceContext, IReadOnlyList<CardModel>, CardModel?, Task>? OnForgottenCards;

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

        /// <summary>
        /// 使用官方钩子 BeforeSideTurnEnd：在玩家回合结束、手牌仍保留时，遗忘所有记忆卡牌。
        /// 只在主机或单人模式下执行，避免联机重复。
        /// </summary>
        public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
        {
            if (side != CombatSide.Player) return;
            if (CombatManager.Instance.IsOverOrEnding) return;

            // 不再判断网络类型，让所有端都执行
            var combatState = CombatManager.Instance.DebugOnlyGetState();
            if (combatState == null) return;

            foreach (var player in combatState.Players)
            {
                var hand = player.PlayerCombatState?.Hand;
                if (hand == null) continue;

                var memoryCards = hand.Cards
                    .Where(c => c.HasModKeyword(CutesakiKeywords.Memory))
                    .ToList();

                if (memoryCards.Count > 0)
                    await MemoryCmd.Forget(choiceContext, memoryCards, null, removeFromMemory: true);
            }
        }
    }
    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterRoomEntered))]
    public static class Hook_AfterRoomEntered_Patch
    {
        public static async void Postfix(IRunState runState, AbstractRoom room)
        {
            // 只在战斗房间时执行初始化
            if (room is not CombatRoom) return;

            // 为拥有 KabutoNote 遗物的玩家初始化记忆牌堆
            foreach (var player in runState.Players)
            {
                if (player.Relics.Any(r => r is KabutoNote))
                    await MemoryCardPile.EnsureInitializedAsync(player);
            }
        }
    }
}