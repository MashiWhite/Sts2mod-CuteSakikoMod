using System.Threading.Tasks;
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

// 1. 加载存档时广播一次（此时不在战斗中，绝对安全）
[HarmonyPatch(typeof(RunSaveManager), nameof(RunSaveManager.LoadRunSave))]
public static class LoadRunSaveSyncPatch
{
    private static void Postfix(ReadSaveResult<SerializableRun> __result)
    {
        if (__result.Success && RunManager.Instance != null && RunManager.Instance.IsInProgress)
        {
            FlybackManager.SyncReloadCountIfHost();
            FlybackManager.SyncPlayCountIfHost();
        }
    }
}

// 2. 进入任何房间时广播一次（仅主机，不等待）
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterRoomEntered))]
public static class AfterRoomEnteredSyncPatch
{
    private static void Postfix(IRunState runState, AbstractRoom room)
    {
        // 只让主机广播，客户端不等待（避免在战斗中干扰招式内同步）
        if (RunManager.Instance.NetService.Type == NetGameType.Host)
        {
            FlybackManager.SyncReloadCountIfHost();
            FlybackManager.SyncPlayCountIfHost();
        }
    }
}

// 3. 每个回合开始时同步一次（所有端参与，保证数据对齐）
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterSideTurnStart))]
public static class AfterSideTurnStartSyncPatch
{
    private static async void Postfix(ICombatState combatState, CombatSide side)
    {
        // 单机模式不执行
        if (RunManager.Instance.NetService.Type == NetGameType.Singleplayer)
            return;

        if (RunManager.Instance.NetService.Type == NetGameType.Host)
        {
            FlybackManager.SyncReloadCountIfHost();
            FlybackManager.SyncPlayCountIfHost();
        }
        else
        {
            await FlybackManager.WaitForDataChange(timeoutMs: 200);
        }
    }
}