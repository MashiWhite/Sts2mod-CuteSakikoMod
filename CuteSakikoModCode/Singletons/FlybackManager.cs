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
    public static PlayerRunSavedData<PlayerFlybackData>? PlayerDataSlot { get; set; }
    public static RunSavedData<RunFlybackData>? RunDataSlot { get; set; }
    public override bool ShouldReceiveCombatHooks => false;
    public static FlybackManager Instance => ModelDb.Singleton<FlybackManager>();

    // ---------- PlayCount 相关字段 ----------
    private static int _cachedTotalPlayCount = 0;
    private static TaskCompletionSource<bool>? _playCountWaitTcs;

    // ---------- ReloadCount 新架构 ----------
    private static int _extraReloadNum = 0;                // 额外重载次数，所有端本地递增
    private static int _baseReloadCount = 0;               // 基础重载次数，客机由主机消息设置
    private static int _lastBroadcastedBaseReload = -1;    // 主机上次广播的基础值，用于去重

    // ---------- 网络环境判断 ----------
    private static bool IsHostOrSingle =>
        RunManager.Instance.NetService?.Type == NetGameType.Host ||
        RunManager.Instance.NetService?.Type == NetGameType.Singleplayer;
    private static bool IsClient =>
        RunManager.Instance.NetService?.Type == NetGameType.Client;

    // ---------- 事件 ----------
    public event Action<int, int>? OnFlybackDataChanged;

    // ---------- TotalPlayCount（保持不变）----------
    public int TotalPlayCount
    {
        get
        {
            if (RunManager.Instance == null || RunManager.Instance.NetService == null || PlayerDataSlot == null)
                return _cachedTotalPlayCount;

            if (IsHostOrSingle)
            {
                int real = CalculateRealTotalPlayCount();
                if (_cachedTotalPlayCount != real)
                {
                    _cachedTotalPlayCount = real;
                    if (RunManager.Instance.NetService.Type == NetGameType.Host)
                        BroadcastPlayCount(real);
                }
                return _cachedTotalPlayCount;
            }
            return _cachedTotalPlayCount;
        }
    }

    // ---------- ReloadCount 新实现 ----------
    public static int GetReloadCount()
    {
        int baseCount = IsHostOrSingle ? GetRawNumReloads() : _baseReloadCount;
        return baseCount + _extraReloadNum;
    }

    // 所有端直接递增额外值（星爱音技能调用）
    public static void IncrementReloadCount()
    {
        Interlocked.Increment(ref _extraReloadNum);
        Instance.NotifyDataChanged();
    }

    // 主机同步基础值给客机（进入房间/重连/回合开始时调用）
    public static void SyncReloadCountIfHost()
    {
        if (!IsHostOrSingle || RunManager.Instance == null || !RunManager.Instance.IsInProgress) return;
        int raw = GetRawNumReloads();
        if (raw == _lastBroadcastedBaseReload) return;
        _lastBroadcastedBaseReload = raw;
        BroadcastReloadCount(raw);
        Instance.NotifyDataChanged();
    }

    // 客机收到基础重载次数
    public static void OnReloadCountReceived(int baseCount)
    {
        if (!IsClient) return;
        _baseReloadCount = baseCount;
        Instance.NotifyDataChanged();
    }

    // ---------- PlayCount 相关方法（保持不变）----------
    public void IncrementPlayCountForPlayer(Player player)
    {
        if (player == null || PlayerDataSlot == null) return;
        PlayerDataSlot.Modify(player, data => data.PlayCount++);

        foreach (var pile in player.Piles)
        foreach (var card in pile.Cards.OfType<Flyback>())
            card.RefreshDynamicVars();

        if (IsHostOrSingle)
        {
            int newTotal = CalculateRealTotalPlayCount();
            if (_cachedTotalPlayCount != newTotal)
            {
                _cachedTotalPlayCount = newTotal;
                if (RunManager.Instance.NetService.Type == NetGameType.Host)
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

        if (IsHostOrSingle)
        {
            int newTotal = Instance.CalculateRealTotalPlayCount();
            _cachedTotalPlayCount = newTotal;
            if (RunManager.Instance.NetService.Type == NetGameType.Host)
                BroadcastPlayCount(newTotal);
            Instance.NotifyDataChanged();
        }
    }

    public static async Task WaitForPlayCountChange(int timeoutMs = 500)
    {
        if (!IsClient) return;
        _playCountWaitTcs?.TrySetResult(false);
        var tcs = new TaskCompletionSource<bool>();
        _playCountWaitTcs = tcs;
        var delayTask = Task.Delay(timeoutMs);
        await Task.WhenAny(tcs.Task, delayTask);
        _playCountWaitTcs = null;
    }

    public static void OnPlayCountReceived(int totalCount)
    {
        if (!IsClient) return;
        _cachedTotalPlayCount = totalCount;
        Instance.NotifyDataChanged();
        _playCountWaitTcs?.TrySetResult(true);
    }

    // ---------- 内部工具 ----------
    private static int GetRawNumReloads()
    {
        var field = typeof(RunManager).GetField("_numReloads",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field != null ? (int)field.GetValue(RunManager.Instance) : 0;
    }

    private int CalculateRealTotalPlayCount()
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null || PlayerDataSlot == null) return 0;
        int total = 0;
        foreach (var player in runState.Players)
            total += PlayerDataSlot.Get(runState, player.NetId).PlayCount;
        return total;
    }

    private static void BroadcastReloadCount(int count)
    {
        if (RunManager.Instance.NetService is NetHostGameService hostService)
            hostService.SendMessage(new ReloadCountSyncMessage { ReloadCount = count });
    }

    private static void BroadcastPlayCount(int totalCount)
    {
        if (RunManager.Instance.NetService is NetHostGameService hostService)
            hostService.SendMessage(new PlayCountSyncMessage { TotalPlayCount = totalCount });
    }

    private void NotifyDataChanged() =>
        OnFlybackDataChanged?.Invoke(TotalPlayCount, GetReloadCount());

    public static void SyncPlayCountIfHost()
    {
        if (!IsHostOrSingle) return;
        int real = Instance.CalculateRealTotalPlayCount();
        _cachedTotalPlayCount = real;
        if (RunManager.Instance.NetService.Type == NetGameType.Host)
            BroadcastPlayCount(real);
        Instance.NotifyDataChanged();
    }
}