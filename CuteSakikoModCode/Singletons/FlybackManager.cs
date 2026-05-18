using System.Reflection;
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
    // 缓存上次同步的 ReloadCount，避免重复广播
    private static int _lastSyncedReloadCount = -1;
    public static PlayerRunSavedData<PlayerFlybackData>? PlayerDataSlot { get; set; }
    public static RunSavedData<RunFlybackData>? RunDataSlot { get; set; }
    public override bool ShouldReceiveCombatHooks => false;
    public static FlybackManager Instance => ModelDb.Singleton<FlybackManager>();

    /// <summary>全局总飞返次数</summary>
    public int TotalPlayCount
    {
        get
        {
            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState == null || PlayerDataSlot == null)
                return 0;

            var total = 0;
            foreach (var player in runState.Players)
                total += PlayerDataSlot.Get(runState, player.NetId).PlayCount;
            return total;
        }
    }

    public int PlayCount => TotalPlayCount;

    public event Action<int, int>? OnFlybackDataChanged;

    public void IncrementPlayCountForPlayer(Player player)
    {
        if (player == null || PlayerDataSlot == null) return;

        PlayerDataSlot.Modify(player, data => data.PlayCount++);

        // 刷新玩家所有牌堆中的Flyback卡牌（除图鉴外）
        foreach (var pile in player.Piles)
        foreach (var card in pile.Cards.OfType<Flyback>())
            card.RefreshDynamicVars();

        NotifyDataChanged();
    }

    public void IncrementPlayCount()
    {
        var player = GetCurrentPlayer();
        if (player != null)
            IncrementPlayCountForPlayer(player);
        else
            Log.Warn("FlybackManager: Cannot increment play count, no player found.");
    }

    public static void DoubleAllPlayerCounts()
    {
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null || PlayerDataSlot == null)
            return;

        foreach (var player in runState.Players)
            PlayerDataSlot.Modify(player, data => data.PlayCount *= 2);

        Instance.NotifyDataChanged();

        // 翻倍后同步主机数据
        SyncReloadCountIfHost();
    }

    /// <summary>获取读档次数（多人时强制同步）</summary>
    public static int GetReloadCount()
    {
        var raw = GetRawNumReloads();

        // 多人模式
        if (IsMultiplayer())
        {
            if (IsHost())
            {
                // 主机：检测变化并广播
                if (_lastSyncedReloadCount != raw)
                {
                    _lastSyncedReloadCount = raw;
                    BroadcastReloadCount(raw);

                    // 同时更新 RunSavedData（用于存档）
                    UpdateRunSavedData(raw);
                }

                return raw;
            }

            // 客户端：从 RunSavedData 读取主机同步的值
            if (RunDataSlot != null)
            {
                var runState = RunManager.Instance.DebugOnlyGetState();
                if (runState != null)
                    return RunDataSlot.Get(runState).ReloadCount;
            }

            return 0; // 兜底
        }

        // 单人模式
        UpdateRunSavedData(raw);
        return raw;
    }

    public static int GetRawNumReloads()
    {
        var field = typeof(RunManager).GetField("_numReloads",
            BindingFlags.NonPublic | BindingFlags.Instance);
        return field != null ? (int)field.GetValue(RunManager.Instance) : 0;
    }

    /// <summary>广播读档次数给所有客户端</summary>
    private static void BroadcastReloadCount(int count)
    {
        if (!IsHost()) return; // 仅主机广播

        var netService = RunManager.Instance.NetService;
        if (netService is NetHostGameService hostService) // 安全转换
        {
            var msg = new ReloadCountSyncMessage { ReloadCount = count };
            hostService.SendMessage(msg); // 广播给所有客户端
        }
    }

    /// <summary>主机主动同步当前读档次数（用于客户端重连等场景）</summary>
    public static void SyncReloadCountIfHost()
    {
        if (!IsHost()) return;

        var raw = GetRawNumReloads();
        _lastSyncedReloadCount = raw;
        UpdateRunSavedData(raw);
        BroadcastReloadCount(raw);
    }

    /// <summary>客户端接收同步数据</summary>
    public static void OnReloadCountReceived(int count)
    {
        if (IsHost()) return; // 主机不处理

        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null || RunDataSlot == null) return;

        var runData = RunDataSlot.Get(runState);
        if (runData.ReloadCount != count)
        {
            runData.ReloadCount = count;
            RunDataSlot.Set(runState, runData);
            Instance.NotifyDataChanged();
        }
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

    private static INetGameService? GetNetService()
    {
        return RunManager.Instance.NetService;
    }

    private static bool IsMultiplayer()
    {
        return GetNetService()?.Type.IsMultiplayer() == true;
    }

    private static bool IsHost()
    {
        return GetNetService()?.Type == NetGameType.Host;
    }

    private Player? GetCurrentPlayer()
    {
        return RunManager.Instance.DebugOnlyGetState()?.Players.FirstOrDefault();
    }

    private void NotifyDataChanged()
    {
        OnFlybackDataChanged?.Invoke(TotalPlayCount, GetReloadCount());
    }
}