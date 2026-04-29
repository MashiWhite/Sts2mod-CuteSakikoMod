using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Scaffolding.Characters;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(NGameOverScreen))]
public static class GameOverFixPatch
{
    // 缓存 ModCharacterTemplate<,,> 的泛型类型定义
    private static readonly Type ModCharacterTemplateGeneric =
        typeof(ModCharacterTemplate<,,>).GetGenericTypeDefinition();

    [HarmonyPrefix]
    [HarmonyPatch("MoveCreaturesToDifferentLayerAndDisableUi")]
    public static bool MoveCreaturesToDifferentLayerAndDisableUi_Prefix(NGameOverScreen __instance)
    {
        if (NCombatRoom.Instance == null)
        {
            var runState = AccessTools.DeclaredField(typeof(NGameOverScreen), "_runState")?.GetValue(__instance);
            if (runState == null) return false;
            var state = runState as RunState;
            if (state == null) return false;
            foreach (var player in state.Players)
            {
                var character = player.Character;
                if (character != null)
                {
                    var type = character.GetType();
                    // 检查类型是否为 ModCharacterTemplate<,,> 或它的派生类
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == ModCharacterTemplateGeneric)
                        return false; // 如果是 ModCharacterTemplate 的任何泛型实例，跳过原方法
                }
            }
        }

        return true;
    }
}