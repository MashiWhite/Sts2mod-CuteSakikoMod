using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(NGameOverScreen))]
public static class GameOverFixPatch
{
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
                if (player.Character is PlaceholderCharacterModel)
                {
                    return false;
                }
            }
        }
        return true;
    }
}