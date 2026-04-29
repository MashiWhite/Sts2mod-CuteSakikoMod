using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using STS2RitsuLib.Scaffolding.Characters;

// 添加命名空间

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(NRestSiteCharacter))]
public static class RestSiteCharacterFlipPatch
{
    private static readonly Type ModCharacterTemplateGeneric =
        typeof(ModCharacterTemplate<,,>).GetGenericTypeDefinition();

    [HarmonyPostfix]
    [HarmonyPatch(nameof(NRestSiteCharacter._Ready))]
    public static void Postfix(NRestSiteCharacter __instance)
    {
        var player = __instance.Player;
        if (player == null) return;

        var character = player.Character;
        if (character == null) return;

        // 检查是否为 ModCharacterTemplate 的任何泛型实例
        if (!IsModCharacterTemplate(character.GetType())) return;

        var players = player.RunState?.Players;
        if (players == null) return;
        var index = players.IndexOf(player);
        if (index < 0) return;

        // 索引为奇数（1,3）时翻转，偶数（0,2）不翻转
        if (index % 2 == 1)
        {
            var sprite = __instance.GetNodeOrNull<Node2D>("Sprite2D");
            if (sprite != null) sprite.Scale = new Vector2(-Mathf.Abs(sprite.Scale.X), sprite.Scale.Y);
        }
    }

    private static bool IsModCharacterTemplate(Type type)
    {
        while (type != null && type != typeof(object))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == ModCharacterTemplateGeneric)
                return true;
            type = type.BaseType;
        }

        return false;
    }
}