using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Systems.Memory;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public sealed class MemoryCardPileManager : HookedSingletonModel
{
    private static readonly CardKeyword MemoryKeyword = CutesakiKeywords.Memory.GetModCardKeyword();

    // 新版基类构造函数：传入 HookType.Combat 以订阅战斗钩子
    public MemoryCardPileManager() : base(HookedSingletonModel.HookType.Combat)
    {
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        MemoryCardPile.Clear();
        ForgetCardPile.Clear();
        await Task.CompletedTask;
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player) return;
        if (CombatManager.Instance.IsOverOrEnding) return;

        var combatState = CombatManager.Instance.DebugOnlyGetState();
        if (combatState == null) return;

        foreach (var player in combatState.Players)
        {
            if (player.Creature.HasPower<AtkByMemoryPower>())
                continue;

            var hand = player.PlayerCombatState?.Hand;
            if (hand == null) continue;

            var memoryCards = hand.Cards
                .Where(c => c.Keywords.Contains(MemoryKeyword))
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
            if (__instance.Keywords.Contains(MemoryKeyword))
                __instance.EnergyCost.AddThisCombat(1);
        }
    }
}