using BaseLib.Abstracts;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

/// <summary>
/// 游戏结束修复补丁
/// 如果你的角色没有Spine可以使用这个来解决非战斗房间放弃的卡死问题
/// </summary>
[HarmonyPatch(typeof(NGameOverScreen))]
public static class GameOverFixPatch
{
    [HarmonyPrefix]
    [HarmonyPatch("MoveCreaturesToDifferentLayerAndDisableUi")]
    public static bool MoveCreaturesToDifferentLayerAndDisableUi_Prefix(NGameOverScreen __instance){
        // 如果在战斗房间或商人房间，则不进行修复
        if(NCombatRoom.Instance == null && NMerchantRoom.Instance == null){
            // 反射获取__instance._runState
            var runState = AccessTools.DeclaredField(typeof(NGameOverScreen), "_runState")?.GetValue(__instance);
            if(runState == null) return false;
            var state = runState as RunState;
            if(state == null) return false;
            // 遍历玩家，如果玩家是PlaceholderCharacterModel，则返回拦截
            foreach (var player in state.Players){
                // 可自行修改判断条件(没有spine动画的角色返回false就行)
                if(player.Character is PlaceholderCharacterModel){
                    return false;
                }
            }
        }
        // 正常运行
        return true;
    }
}