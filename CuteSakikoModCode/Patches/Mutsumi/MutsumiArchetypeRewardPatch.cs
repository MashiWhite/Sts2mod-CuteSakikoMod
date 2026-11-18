using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Archetypes;
using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Patches.Mutsumi;

/// <summary>
///     睦的流派卡池过滤骨架：不换 CardPool 对象，只在奖励生成时给
///     <see cref="CardCreationOptions.CardPoolFilter" /> 叠一层「通用卡 + 当前流派卡」。
///     覆盖战斗奖励、商店、事件奖励（它们最终都走 CardFactory.CreateForReward）。
/// </summary>
[HarmonyPatch(typeof(CardFactory), nameof(CardFactory.CreateForReward),
    [typeof(Player), typeof(int), typeof(CardCreationOptions)])]
public static class MutsumiArchetypeRewardPatch
{
    private static void Prefix(Player player, CardCreationOptions options)
    {
        if (player?.Character is not CuteMutsumi) return;

        var choice = MutsumiArchetypeChoice.Get(player);
        var previous = options.CardPoolFilter;
        Func<CardModel, bool> filter = card => (previous?.Invoke(card) ?? true) && IsAllowed(card, choice);
#if CSK_GAME_111_OR_NEWER
        options.WithFilter(filter);
#else
        options.WithCardPools(options.CardPools, filter);
#endif
    }

    private static bool IsAllowed(CardModel card, MutsumiArchetype choice)
    {
        return card is not IMutsumiArchetypeCard tagged || tagged.Archetype == choice;
    }
}
