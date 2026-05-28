using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Token;
using CuteSakikoMod.CuteSakikoModCode.NetMessage;
using MegaCrit.Sts2.Core.Entities.Players;
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
    // ---------- ReloadCount 字段 ----------
    private static int _extraReloadNum;
    private static int _baseReloadCount;
    private static int _lastBroadcastedBaseReload = -1;
    public static PlayerRunSavedData<PlayerFlybackData>? PlayerDataSlot { get; set; }
    public static RunSavedData<RunFlybackData>? RunDataSlot { get; set; }
    public override bool ShouldReceiveCombatHooks => false;
    public static FlybackManager Instance => ModelDb.Singleton<FlybackManager>();

    private static bool IsHostOrSingle =>
        RunManager.Instance.NetService?.Type == NetGameType.Host ||
        RunManager.Instance.NetService?.Type == NetGameType.Singleplayer;

    private static bool IsClient =>
        RunManager.Instance.NetService?.Type == NetGameType.Client;

    // ---------- TotalPlayCount：实时从所有玩家数据计算 ----------
    public int TotalPlayCount
    {
        get
        {
            var runState = RunManager.Instance?.DebugOnlyGetState();
            if (runState == null || PlayerDataSlot == null) return 0;
            var total = 0;
            foreach (var player in runState.Players)
                total += PlayerDataSlot.Get(runState, player.NetId).PlayCount;
            return total;
        }
    }

    public event Action<int, int>? OnFlybackDataChanged;

    // ---------- ReloadCount ----------
    public static int GetReloadCount()
    {
        var baseCount = IsHostOrSingle ? GetRawNumReloads() : _baseReloadCount;
        return baseCount + _extraReloadNum;
    }

    public static void IncrementReloadCount()
    {
        Interlocked.Increment(ref _extraReloadNum);
        Instance.NotifyDataChanged();
    }

    public static void SyncReloadCountIfHost()
    {
        if (!IsHostOrSingle || RunManager.Instance == null || !RunManager.Instance.IsInProgress) return;
        var raw = GetRawNumReloads();
        if (raw == _lastBroadcastedBaseReload) return;
        _lastBroadcastedBaseReload = raw;
        if (RunManager.Instance.NetService is NetHostGameService hostService)
            hostService.SendMessage(new ReloadCountSyncMessage { ReloadCount = raw });
        Instance.NotifyDataChanged();
    }

    public static void OnReloadCountReceived(int baseCount)
    {
        if (!IsClient) return;
        _baseReloadCount = baseCount;
        Instance.NotifyDataChanged();
    }

    // ---------- PlayCount 修改（完全本地，不再网络广播）----------
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
        var runState = RunManager.Instance.DebugOnlyGetState();
        if (runState == null || PlayerDataSlot == null) return;
        foreach (var player in runState.Players)
            PlayerDataSlot.Modify(player, data => data.PlayCount *= 2);

        Instance.NotifyDataChanged();
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
}