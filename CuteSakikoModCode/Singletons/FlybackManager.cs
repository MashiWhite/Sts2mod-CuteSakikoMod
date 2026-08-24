using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Event;
using CuteSakikoMod.CuteSakikoModCode.NetMessage;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;
using STS2RitsuLib.RunData;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public class FlybackManager : HookedSingletonModel
{
    public FlybackManager() : base(HookType.None) { }

    // ---------- 数据槽位 ----------
    public static PlayerRunSavedData<PlayerFlybackData>? PlayerDataSlot { get; set; }
    public static RunSavedData<RunFlybackData>? RunDataSlot { get; set; }

    private static bool IsHostOrSingle =>
        RunManager.Instance.NetService?.Type == NetGameType.Host ||
        RunManager.Instance.NetService?.Type == NetGameType.Singleplayer;

    private static bool IsClient =>
        RunManager.Instance.NetService?.Type == NetGameType.Client;

    public static FlybackManager Instance => ModelDb.Singleton<FlybackManager>();

    // ---------- 获取当前 RunState ----------
    private static RunState? GetCurrentRunState()
    {
        return RunManager.Instance?.DebugOnlyGetState();
    }

    // ---------- ReloadCount 操作（存储到 RunSavedData）----------
    public static int GetReloadCount()
    {
        var runState = GetCurrentRunState();
        if (runState == null || RunDataSlot == null) return 0;

        var data = RunDataSlot.Get(runState);
        var baseCount = IsHostOrSingle ? GetRawNumReloads() : data.BaseReloadCount;
        return baseCount + data.ExtraReloadNum;
    }

    public static void IncrementReloadCount()
    {
        var runState = GetCurrentRunState();
        if (runState == null || RunDataSlot == null) return;

        RunDataSlot.Modify(runState, data => data.ExtraReloadNum++);
        Instance.NotifyDataChanged();
    }

    public static void SyncReloadCountIfHost()
    {
        if (!IsHostOrSingle || RunManager.Instance == null || !RunManager.Instance.IsInProgress) return;
        var raw = GetRawNumReloads();
        var runState = GetCurrentRunState();
        if (runState == null || RunDataSlot == null) return;

        var currentBase = RunDataSlot.Get(runState).BaseReloadCount;
        if (raw == currentBase) return;

        RunDataSlot.Modify(runState, data => data.BaseReloadCount = raw);
        if (RunManager.Instance.NetService is NetHostGameService hostService)
            hostService.SendMessage(new ReloadCountSyncMessage { ReloadCount = raw });
        Instance.NotifyDataChanged();
    }

    public static void OnReloadCountReceived(int baseCount)
    {
        if (!IsClient) return;
        var runState = GetCurrentRunState();
        if (runState == null || RunDataSlot == null) return;

        RunDataSlot.Modify(runState, data => data.BaseReloadCount = baseCount);
        Instance.NotifyDataChanged();
    }

    // ---------- PlayCount 操作 ----------
    public void IncrementPlayCountForPlayer(Player player)
    {
        if (player == null || PlayerDataSlot == null) return;
        PlayerDataSlot.Modify(player, data => data.PlayCount++);

        foreach (var pile in player.Piles)
        foreach (var card in pile.Cards.OfType<Flyback>())
            card.RefreshDynamicVars();

        NotifyDataChanged();
    }

    public static void DoubleAllPlayerCounts()
    {
        var runState = GetCurrentRunState();
        if (runState == null || PlayerDataSlot == null) return;
        foreach (var player in runState.Players)
            PlayerDataSlot.Modify(player, data => data.PlayCount *= 2);
        Instance.NotifyDataChanged();
    }

    // ---------- TotalPlayCount ----------
    public int TotalPlayCount
    {
        get
        {
            var runState = GetCurrentRunState();
            if (runState == null || PlayerDataSlot == null) return 0;
            var total = 0;
            foreach (var player in runState.Players)
                total += PlayerDataSlot.Get(runState, player.NetId).PlayCount;
            return total;
        }
    }

    // ---------- 内部工具 ----------
    private static int GetRawNumReloads()
    {
        var field = typeof(RunManager).GetField("_numReloads",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field != null ? (int)field.GetValue(RunManager.Instance) : 0;
    }

    private void NotifyDataChanged()
    {
        OnFlybackDataChanged?.Invoke(TotalPlayCount, GetReloadCount());
    }

    public event Action<int, int>? OnFlybackDataChanged;
}