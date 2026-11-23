using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Archetypes;
using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Core;
using MegaCrit.Sts2.Core.Entities.Players;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

/// <summary>
///     睦的流派/卡池选择。实际值存在皮肤系统的 CharacterSkinChoice 里，
///     跟随大厅/跑局存档，并通过 SkinSyncService 同步给其他玩家。
/// </summary>
public static class MutsumiArchetypeChoice
{
    public static MutsumiArchetype Get(Player? player)
    {
        if (player == null) return MutsumiArchetype.Cucumber;

        var choice = SkinResolver.GetChoiceForPlayer(
            player.NetId,
            player.Character.GetType());

        return choice.MutsumiArchetypeIndex == (int)MutsumiArchetype.Mortis
            ? MutsumiArchetype.Mortis
            : MutsumiArchetype.Cucumber;
    }

    public static void Set(Player player, MutsumiArchetype archetype)
    {
        if (player == null) return;

        SkinResolver.ModifyLocalChoice(
            player.Character.GetType(),
            c => c.MutsumiArchetypeIndex = (int)archetype);
    }
}
