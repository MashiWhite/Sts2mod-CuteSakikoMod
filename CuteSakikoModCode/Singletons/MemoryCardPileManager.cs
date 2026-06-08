using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public sealed class MemoryCardPileManager : HookedSingletonModel
{
    private static readonly CardKeyword MemoryKeyword = CutesakiKeywords.Memory.GetModCardKeyword();

    public MemoryCardPileManager() : base(true, false)
    {
    }

    public override bool ShouldReceiveCombatHooks => true;

    public static event Func<PlayerChoiceContext, IReadOnlyList<CardModel>, CardModel?, Task>? OnForgottenCards;

    internal static Task FireOnForgottenCards(PlayerChoiceContext choiceContext, IReadOnlyList<CardModel> cards,
        CardModel? source)
    {
        return OnForgottenCards != null ? OnForgottenCards.Invoke(choiceContext, cards, source) : Task.CompletedTask;
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        MemoryCardPile.Clear();
        ForgetCardPile.Clear();
        await Task.CompletedTask;
    }

    /// <summary>
    ///     使用官方钩子 BeforeSideTurnEnd：在玩家回合结束、手牌仍保留时，遗忘所有记忆卡牌。
    ///     只在主机或单人模式下执行，避免联机重复。
    /// </summary>
    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player) return;
        if (CombatManager.Instance.IsOverOrEnding) return;

        var combatState = CombatManager.Instance.DebugOnlyGetState();
        if (combatState == null) return;

        foreach (var player in combatState.Players)
        {
            // 如果玩家拥有 AtkByMemoryPower，则本回合不遗忘记忆卡牌
            if (player.Creature.HasPower<AtkByMemoryPower>())
                continue;

            var hand = player.PlayerCombatState?.Hand;
            if (hand == null) continue;

            var memoryCards = hand.Cards
                .Where(c => c.Keywords.Contains(CutesakiKeywords.Memory.GetModCardKeyword()))
                .ToList();

            if (memoryCards.Count > 0)
                await MemoryCmd.Forget(choiceContext, memoryCards);
        }
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.OnPlayWrapper))]
    private static class CardModel_OnPlayWrapper_Patch
    {
        public static void Postfix(CardModel __instance)
        {
            if (__instance.Keywords.Contains(CutesakiKeywords.Memory.GetModCardKeyword()))
                __instance.EnergyCost.AddThisCombat(1);
        }
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterRoomEntered))]
public static class Hook_AfterRoomEntered_Patch
{
    public static async void Postfix(IRunState runState, AbstractRoom room)
    {
        if (room is not CombatRoom) return;
        // 关键：为所有玩家初始化记忆牌堆（不再依赖是否拥有 KabutoNote）
        foreach (var player in runState.Players)
        {
            await MemoryCardPile.EnsureInitializedAsync(player);
        }
    }
}