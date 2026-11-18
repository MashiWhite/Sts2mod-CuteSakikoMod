using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Archetypes;
using MegaCrit.Sts2.Core.Entities.Players;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

/// <summary>
///     睦的流派选择（骨架版）：目前只存在内存里，默认黄瓜睦。
///     TODO：接入皮肤系统的 PlayerChoice 存档 + SkinSyncService 联机同步。
/// </summary>
public static class MutsumiArchetypeChoice
{
    private static readonly Dictionary<ulong, MutsumiArchetype> Choices = new();

    public static MutsumiArchetype Get(Player? player)
    {
        if (player == null) return MutsumiArchetype.Cucumber;
        return Choices.TryGetValue(player.NetId, out var value) ? value : MutsumiArchetype.Cucumber;
    }

    public static void Set(Player player, MutsumiArchetype archetype)
    {
        if (player == null) return;
        Choices[player.NetId] = archetype;
    }

    public static void Clear() => Choices.Clear();
}
