using CuteSakikoMod.CuteSakikoModCode.Singletons;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

// 1. 加载存档时广播一次基础 ReloadCount（主机）
[HarmonyPatch(typeof(RunSaveManager), nameof(RunSaveManager.LoadRunSave))]
public static class LoadRunSaveSyncPatch
{
    private static void Postfix(ReadSaveResult<SerializableRun> __result)
    {
        if (__result.Success && RunManager.Instance != null && RunManager.Instance.IsInProgress)
            FlybackManager.SyncReloadCountIfHost();
    }
}

// 2. 进入任何房间时广播一次基础 ReloadCount（主机）
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterRoomEntered))]
public static class AfterRoomEnteredSyncPatch
{
    private static void Postfix(IRunState runState, AbstractRoom room)
    {
        if (RunManager.Instance.NetService.Type == NetGameType.Host) FlybackManager.SyncReloadCountIfHost();
    }
}

// 3. 每个回合开始时，主机广播基础 ReloadCount（客机无需等待）
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterSideTurnStart))]
public static class AfterSideTurnStartSyncPatch
{
    private static void Postfix(ICombatState combatState, CombatSide side)
    {
        if (RunManager.Instance.NetService.Type == NetGameType.Singleplayer)
            return;

        if (RunManager.Instance.NetService.Type == NetGameType.Host) FlybackManager.SyncReloadCountIfHost();
    }
}