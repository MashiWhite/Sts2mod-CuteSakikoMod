using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Token;
using CuteSakikoMod.CuteSakikoModCode.NetMessage;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.RunData;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public class FlybackManager : SingletonModel
{
    private static int _lastSyncedReloadCount = -1;
    private static int _cachedTotalPlayCount = 0;
    public static PlayerRunSavedData<PlayerFlybackData>? PlayerDataSlot { get; set; }
    public static RunSavedData<RunFlybackData>? RunDataSlot { get; set; }
    public override bool ShouldReceiveCombatHooks => false;
    public static FlybackManager Instance => ModelDb.Singleton<FlybackManager>();

    private static TaskCompletionSource<bool>? _reloadCountWaitTcs;
    private static int _reloadCountWaitExpected;
    private static int _playCountVersion = 0;          // 用于等待 PlayCount 变化

    public int TotalPlayCount
    {
        get
        {
            if (RunManager.Instance.NetService.Type == NetGameType.Host)
            {
                int real = CalculateRealTotalPlayCount();
                if (_cachedTotalPlayCount != real)
                {
                    _cachedTotalPlayCount = real;
                    BroadcastPlayCount(real);
                }
                return _cachedTotalPlayCount;
            }
            return _cachedTotalPlayCount;
        }
    }

    public int PlayCount => TotalPlayCount;

    public event Action<int, int>? OnFlybackDataChanged;

    public void IncrementPlayCountForPlayer(Player player)
    {
        if (player == null || PlayerDataSlot == null) return;

        PlayerDataSlot.Modify(player, data => data.PlayCount++);

        foreach (var pile in player.Piles)
        foreach (var card in pile.Cards.OfType<Flyback>())
            card.RefreshDynamicVars();

        if (RunManager.Instance.NetService.Type == NetGameType.Host)
        {
            int newTotal = CalculateRealTotalPlayCount();
            if (_cachedTotalPlayCount != newTotal)
            {
                _cachedTotalPlayCount = newTotal;
                BroadcastPlayCount(newTotal);
                NotifyDataChanged();
            }
        }
    }

    public static void DoubleAllPlayerCounts()
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null || PlayerDataSlot == null) return;

        foreach (var player in runState.Players)
            PlayerDataSlot.Modify(player, data => data.PlayCount *= 2);

        if (RunManager.Instance.NetService.Type == NetGameType.Host)
        {
            int newTotal = Instance.CalculateRealTotalPlayCount();
            _cachedTotalPlayCount = newTotal;
            BroadcastPlayCount(newTotal);
            Instance.NotifyDataChanged();
        }
    }

    // ---------- ReloadCount 相关 ----------
    public static void IncrementReloadCount()
    {
        if (RunManager.Instance.NetService.Type != NetGameType.Host) return;

        var field = typeof(RunManager).GetField("_numReloads", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) return;
        int current = (int)field.GetValue(RunManager.Instance);
        field.SetValue(RunManager.Instance, current + 1);
        if (RunDataSlot != null)
        {
            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState != null)
                RunDataSlot.Modify(runState, data => data.ReloadCount = current + 1);
        }
        _lastSyncedReloadCount = current + 1;
        BroadcastReloadCount(current + 1);
        Instance.NotifyDataChanged();
    }

    public static int GetReloadCount()
    {
        if (RunManager.Instance.NetService.Type == NetGameType.Host)
        {
            int raw = GetRawNumReloads();
            UpdateRunSavedData(raw);
            return raw;
        }
        if (RunDataSlot != null)
        {
            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState != null) return RunDataSlot.Get(runState).ReloadCount;
        }
        return 0;
    }

    public static int GetRawNumReloads()
    {
        var field = typeof(RunManager).GetField("_numReloads", BindingFlags.NonPublic | BindingFlags.Instance);
        return field != null ? (int)field.GetValue(RunManager.Instance) : 0;
    }

    private static void BroadcastReloadCount(int count)
    {
        if (RunManager.Instance.NetService is NetHostGameService hostService)
            hostService.SendMessage(new ReloadCountSyncMessage { ReloadCount = count });
    }

    public static void SyncReloadCountIfHost()
    {
        if (Instance == null || RunManager.Instance == null || !RunManager.Instance.IsInProgress) return;
        if (RunManager.Instance.NetService.Type != NetGameType.Host) return;
        if (RunManager.Instance.NetService is not NetHostGameService hostService || !hostService.IsConnected) return;
        int raw = GetRawNumReloads();
        if (_lastSyncedReloadCount == raw) return;
        _lastSyncedReloadCount = raw;
        UpdateRunSavedData(raw);
        BroadcastReloadCount(raw);
        Instance.NotifyDataChanged();
    }

    public static void OnReloadCountReceived(int count)
    {
        if (RunManager.Instance.NetService.Type == NetGameType.Host) return;
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null || RunDataSlot == null) return;
        var runData = RunDataSlot.Get(runState);
        if (runData.ReloadCount != count)
        {
            runData.ReloadCount = count;
            RunDataSlot.Set(runState, runData);
            Instance.NotifyDataChanged();
            if (_reloadCountWaitTcs != null && GetReloadCount() >= _reloadCountWaitExpected)
                _reloadCountWaitTcs.TrySetResult(true);
        }
    }

    // ---------- PlayCount 广播与接收 ----------
    private static void BroadcastPlayCount(int totalCount)
    {
        if (RunManager.Instance.NetService is NetHostGameService hostService)
            hostService.SendMessage(new PlayCountSyncMessage { TotalPlayCount = totalCount });
    }

    public static void OnPlayCountReceived(int totalCount)
    {
        if (RunManager.Instance.NetService.Type == NetGameType.Host) return;
        _cachedTotalPlayCount = totalCount;
        _playCountVersion++;                // 递增版本号
        Instance.NotifyDataChanged();
    }

    // ---------- 等待方法 ----------
    public static async Task WaitForReloadCountV2(int expected, int timeoutMs = 1000)
    {
        if (RunManager.Instance.NetService.Type != NetGameType.Client || GetReloadCount() >= expected) return;
        var tcs = new TaskCompletionSource<bool>();
        _reloadCountWaitTcs = tcs;
        _reloadCountWaitExpected = expected;
        var delayTask = Task.Delay(timeoutMs);
        var completedTask = await Task.WhenAny(tcs.Task, delayTask);
        _reloadCountWaitTcs = null;
        if (completedTask == delayTask)
            Log.Warn($"WaitForReloadCountV2 timed out, current: {GetReloadCount()}, expected: {expected}");
    }

    /// <summary>等待一次 PlayCount 更新（版本号变化），无竞态。</summary>
    public static async Task WaitForPlayCountChange(int timeoutMs = 500)
    {
        if (RunManager.Instance.NetService.Type != NetGameType.Client) return;

        int startVersion = _playCountVersion;
        // 先检查一次，如果在调用前已经更新，直接返回
        if (_playCountVersion != startVersion) return;

        var tcs = new TaskCompletionSource<bool>();
        Action<int, int>? handler = null;
        handler = (_, _) =>
        {
            if (_playCountVersion != startVersion)
            {
                Instance.OnFlybackDataChanged -= handler;
                tcs.TrySetResult(true);
            }
        };

        Instance.OnFlybackDataChanged += handler;
        // 再次检查，防止在订阅前版本已经变化
        if (_playCountVersion != startVersion)
        {
            Instance.OnFlybackDataChanged -= handler;
            return;
        }
        var delayTask = Task.Delay(timeoutMs);
        var completedTask = await Task.WhenAny(tcs.Task, delayTask);
        Instance.OnFlybackDataChanged -= handler;
    }

    // 兼容旧接口
    public static async Task WaitForReloadCount(int expected, int timeoutMs = 1000) =>
        await WaitForReloadCountV2(expected, timeoutMs);
    public static async Task WaitForDataChange(int timeoutMs = 500) =>
        await WaitForPlayCountChange(timeoutMs);

    // ---------- 辅助方法 ----------
    private int CalculateRealTotalPlayCount()
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null || PlayerDataSlot == null) return 0;
        int total = 0;
        foreach (var player in runState.Players)
            total += PlayerDataSlot.Get(runState, player.NetId).PlayCount;
        return total;
    }

    private static void UpdateRunSavedData(int count)
    {
        if (RunDataSlot == null) return;
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null) return;
        var runData = RunDataSlot.Get(runState);
        if (runData.ReloadCount != count)
        {
            runData.ReloadCount = count;
            RunDataSlot.Set(runState, runData);
        }
    }

    private Player? GetCurrentPlayer() =>
        RunManager.Instance.DebugOnlyGetState()?.Players.FirstOrDefault();

    private void NotifyDataChanged() =>
        OnFlybackDataChanged?.Invoke(TotalPlayCount, GetReloadCount());

    public static void SyncPlayCountIfHost()
    {
        if (RunManager.Instance.NetService.Type != NetGameType.Host) return;
        int real = Instance.CalculateRealTotalPlayCount();
        _cachedTotalPlayCount = real;
        BroadcastPlayCount(real);
        Instance.NotifyDataChanged();
    }
}