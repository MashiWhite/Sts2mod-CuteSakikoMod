using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Events;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

// ========== 原生互斥：修正索引并移除其他普通选项 ==========
[HarmonyPatch(typeof(RestSiteSynchronizer), "ChooseOption")]
public static class RestSiteOption_ChooseOption_Patch
{
    [HarmonyPrefix]
    private static bool Prefix(Player player, ref int optionIndex, ref Task<bool> __result)
    {
        var sync = RunManager.Instance.RestSiteSynchronizer;
        if (sync == null) return true;

        // ---- 反射获取该玩家的选项列表 ----
        var restSitesField = typeof(RestSiteSynchronizer).GetField("_restSites",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (restSitesField == null) return true;

        var restSites = restSitesField.GetValue(sync) as System.Collections.IList;
        if (restSites == null) return true;

        var state = RunManager.Instance.DebugOnlyGetState();
        if (state == null) return true;

        var playerCollection = state as IPlayerCollection;
        if (playerCollection == null) return true;

        int slotIndex = playerCollection.GetPlayerSlotIndex(player);
        if (slotIndex < 0 || slotIndex >= restSites.Count) return true;

        var playerRestSite = restSites[slotIndex];
        var optionsField = playerRestSite.GetType().GetField("options",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (optionsField == null) return true;

        if (optionsField.GetValue(playerRestSite) is not List<RestSiteOption> options) return true;
        if (optionIndex >= options.Count) return true;

        var chosenOption = options[optionIndex];

        // 练习吉他完全独立，不参与互斥
        if (chosenOption is PracticeGuitarOption) return true;

        // 有帐篷时不限制（允许选多个）
        var me = LocalContext.GetMe(state);
        if (me?.Relics.OfType<MiniatureTent>().Any() == true) return true;

        // ---- 移除其他非练习吉他选项 ----
        var toRemove = options
            .Where(o => o != chosenOption && o is not PracticeGuitarOption)
            .ToList();

        foreach (var opt in toRemove)
            options.Remove(opt);

        // ---- 重新计算当前选项的新索引 ----
        int newIndex = options.IndexOf(chosenOption);
        if (newIndex < 0)
        {
            // 理论上不会发生，安全兜底
            __result = Task.FromResult(false);
            return false;
        }

        optionIndex = newIndex;
        RitsuLibFramework.Logger.Info(
            $"[RestSitePatch] Intercepted {chosenOption.OptionId}, removed {toRemove.Count} other options, new index: {newIndex}");
        return true;
    }
}

// ========== 进入休息处重置标记 ==========
[HarmonyPatch(typeof(RestSiteRoom))]
public static class RestSiteRoomEnterPatch
{
    [HarmonyPatch("EnterInternal")]
    [HarmonyPostfix]
    public static void OnEnterRestSiteRoom(AbstractRoom __instance, IRunState? runState, bool isRestoringRoomStackBase)
    {
        if (runState == null) return;
        RitsuLibFramework.Logger.Info("Entered RestSiteRoom, resetting flags");

        foreach (var player in runState.Players)
        {
            var guitar = player.Relics.OfType<AnonGuitar>().FirstOrDefault();
            if (guitar != null)
            {
                guitar.NormalOptionUsed = false;
                guitar.PracticeUsedThisVisit = false;
            }
        }
    }
}