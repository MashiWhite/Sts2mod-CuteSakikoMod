using HarmonyLib;
using MegaCrit.Sts2.Core.Platform;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Systems;

namespace CuteSakikoMod.CuteSakikoModCode.Patches
{
    [HarmonyPatch(typeof(PlatformUtil), nameof(PlatformUtil.GetPlayerNameRaw))]
    [HarmonyPriority(Priority.Last)]
    public static class PlayerNamePatch
    {
        // ⚠️ 第一个参数名必须与原始方法完全一致：platformType
        public static bool Prefix(PlatformType platformType, ulong playerId, ref string __result)
        {
            var runState = NameChangeCmd.GetCurrentRunState();
            if (runState == null)
                return true;

            var data = PlayerNameData.PlayerNameSlot.Get(runState, playerId);
            if (data != null && !string.IsNullOrEmpty(data.CustomName))
            {
                __result = data.CustomName;
                return false;
            }

            return true;
        }
    }
}