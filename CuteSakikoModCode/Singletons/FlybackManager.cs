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
    private static TaskCompletionSource<bool>? _playCountWaitTcs;

    public int TotalPlayCount
    {
        get
        {
            // 全面保护：RunManager、NetService 或 PlayerDataSlot 未就绪时，返回缓存值（通常为0）
            if (RunManager.Instance == null || RunManager.Instance.NetService == null || PlayerDataSlot == null)
                return _cachedTotalPlayCount;

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
        // 全面保护：RunManager 或 RunDataSlot 未就绪时返回 0
        if (RunManager.Instance == null || RunDataSlot == null)
            return 0;

        if (RunManager.Instance.NetService == null)
            return 0;

        if (RunManager.Instance.NetService.Type == NetGameType.Host)
        {
            int raw = GetRawNumReloads();
            UpdateRunSavedData(raw);
            return raw;
        }
        // 客户端
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState != null)
            return RunDataSlot.Get(runState).ReloadCount;
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
        Instance.NotifyDataChanged();
        _playCountWaitTcs?.TrySetResult(true);
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

    public static async Task WaitForPlayCountChange(int timeoutMs = 500)
    {
        if (RunManager.Instance.NetService.Type != NetGameType.Client) return;

        _playCountWaitTcs?.TrySetResult(false);
        var tcs = new TaskCompletionSource<bool>();
        _playCountWaitTcs = tcs;

        var delayTask = Task.Delay(timeoutMs);
        var completedTask = await Task.WhenAny(tcs.Task, delayTask);
        _playCountWaitTcs = null;
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