using System.Text.Json;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Networking.Sidecar;
using STS2RitsuLib.RunData;

namespace CuteSakikoMod.CuteSakikoModCode.Others.Config;

public static class GameplayConfigSync
{
    public const string TopicId = "cute_sakiko_gameplay";

    public static RunSavedData<RunGameplayConfigData> RunConfigSlot = null!;

    private static bool _applyingRemote;

    private static IDisposable? _handshakeSub;
    private static IDisposable? _topicChangedSub;
    private static IDisposable? _sessionBoundSub;
    private static IDisposable? _sessionUnboundSub;
    private static IDisposable? _runDataPreparingSub;
    private static bool _runStartedSubscribed;

    public static bool IsHostAuthority { get; private set; }

    public static bool ShouldLockGameplaySettings
    {
        get
        {
            var rm = RunManager.Instance;
            var ns = rm?.NetService;
            if (ns == null) return false;
            if (ns.Type != NetGameType.Client) return false;
            return rm!.DebugOnlyGetState() != null;
        }
    }

    public static void Init()
    {
        if (RunConfigSlot == null)
            throw new InvalidOperationException(
                "[GameplayConfigSync] RunConfigSlot 必须在 Init() 之前注册。");

        UpdateAuthority(RunManager.Instance?.NetService);

        RitsuLibSidecarConfigSyncService.RegisterTopic<GameplayConfigDto, GameplayConfigDelta>(
            TopicId,
            GameplayConfigDto.FromConfig(LoadConfig()),
            CanClientRequest,
            ApplyDelta);

        _topicChangedSub ??= RitsuLibSidecarEvents.OnConfigTopicChanged(OnTopicChanged);

        _sessionBoundSub ??= RitsuLibSidecarEvents.OnSessionBound(evt =>
        {
            UpdateAuthority(evt.NetService);
            Entry.Logger.Info(
                $"[ConfigSync] SessionBound: type={evt.NetService.Type} IsHostAuthority={IsHostAuthority}");
        });

        _sessionUnboundSub ??= RitsuLibSidecarEvents.OnSessionUnbound(_ =>
        {
            UpdateAuthority(null);
            Entry.Logger.Info($"[ConfigSync] SessionUnbound: IsHostAuthority={IsHostAuthority}");
        });

        if (!_runStartedSubscribed)
        {
            _runStartedSubscribed = true;
            RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(OnRunStarted);
            _runDataPreparingSub ??= RitsuLibFramework.SubscribeLifecycle<RunSavedDataPreparingEvent>(OnRunSavedDataPreparing);
        }
    }

    public static void OnNetServiceReady(INetGameService? netService)
    {
        UpdateAuthority(netService);

        if (IsHostAuthority)
        {
            EnsureHandshakeSubscription();
            return;
        }

        TryApplyRunSnapshotConfig();
    }

    private static void UpdateAuthority(INetGameService? netService)
    {
        IsHostAuthority = netService == null
                          || netService.Type is NetGameType.Singleplayer or NetGameType.Host;
    }

    private static void EnsureHandshakeSubscription()
    {
        _handshakeSub ??= RitsuLibSidecarEvents.OnHandshakeCompleted(_ => BroadcastHostState("handshake"));
    }

    // ========== RunSnapshot 写入 ==========

    private static void OnRunSavedDataPreparing(RunSavedDataPreparingEvent evt)
    {
        if (!IsHostAuthority) return;
        Entry.Logger.Info("[ConfigSync] RunSavedDataPreparingEvent: 房主写入 snapshot（导出前）");
        TryWriteRunSnapshotConfig(evt.RunState);
    }

    private static void OnRunStarted(RunStartedEvent evt)
    {
        if (!IsHostAuthority) return;

        Entry.Logger.Info($"[ConfigSync] RunStartedEvent: IsMultiplayer={evt.IsMultiplayer}");

        TryWriteRunSnapshotConfig(evt.RunState);
        BroadcastHostState("run_started");
    }

    private static void TryWriteRunSnapshotConfig(RunState state)
    {
        try
        {
            var cfg = LoadConfig();
            Entry.Logger.Info(
                $"[ConfigSync] 房主写入 run snapshot: Eggs={cfg.EggsCard} Monsters={cfg.EnableModMonsters} " +
                $"Ancients={cfg.EnableCustomAncients} Events={cfg.EnableCustomEvents} " +
                $"ScalePressure={cfg.ScalePressureInMultiplayer}");

            RunConfigSlot.Modify(state, data => data.CopyFrom(cfg));
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"[ConfigSync] 房主写 snapshot 失败: {ex.Message}");
        }
    }

    // ========== RunSnapshot 读取 ==========

    private static void TryApplyRunSnapshotConfig()
    {
        try
        {
            var state = RunManager.Instance?.DebugOnlyGetState();
            if (state == null)
            {
                Entry.Logger.Warn("[ConfigSync] run snapshot 读取失败: RunState 为 null");
                return;
            }

            var data = RunConfigSlot.Get(state);
            if (data == null)
            {
                Entry.Logger.Warn("[ConfigSync] run snapshot 里没有 GameplayConfigSnapshot 数据");
                return;
            }

            Entry.Logger.Info(
                $"[ConfigSync] run snapshot 读到: Eggs={data.EggsCard} Monsters={data.EnableModMonsters} " +
                $"Ancients={data.EnableCustomAncients} Events={data.EnableCustomEvents} " +
                $"ScalePressure={data.ScalePressureInMultiplayer}");

            _applyingRemote = true;
            try
            {
                var cfg = LoadConfig();
                data.ApplyTo(cfg);
                // 只改内存，不落盘：客机的 config.json 属于玩家自己，
                // 联机时临时采用房主配置，下次单机仍用玩家原来的设置。
                Entry.Logger.Info(
                    $"[ConfigSync] 从 run snapshot 应用房主配置: Eggs={cfg.EggsCard} Monsters={cfg.EnableModMonsters} " +
                    $"ScalePressure={cfg.ScalePressureInMultiplayer}");
            }
            finally
            {
                _applyingRemote = false;
            }
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"[ConfigSync] run snapshot 应用失败: {ex.Message}");
        }
    }

    // ========== Sidecar ==========

    private static bool CanClientRequest(ulong sender, GameplayConfigDelta delta)
    {
        var netService = RunManager.Instance?.NetService;
        if (netService == null) return true;
        return sender == netService.NetId;
    }

    private static GameplayConfigDto ApplyDelta(GameplayConfigDto current, GameplayConfigDelta delta)
    {
        var next = current.Clone();
        if (delta.EggsCard.HasValue) next.EggsCard = delta.EggsCard.Value;
        if (delta.EnableModMonsters.HasValue) next.EnableModMonsters = delta.EnableModMonsters.Value;
        if (delta.EnableCustomAncients.HasValue) next.EnableCustomAncients = delta.EnableCustomAncients.Value;
        if (delta.EnableCustomEvents.HasValue) next.EnableCustomEvents = delta.EnableCustomEvents.Value;
        if (delta.ScalePressureInMultiplayer.HasValue) next.ScalePressureInMultiplayer = delta.ScalePressureInMultiplayer.Value;
    
        if (delta.UseModWeight.HasValue) next.UseModWeight = delta.UseModWeight.Value;
        if (delta.UseModAncientWeight.HasValue) next.UseModAncientWeight = delta.UseModAncientWeight.Value;
        if (delta.ModAncientWeight.HasValue) next.ModAncientWeight = delta.ModAncientWeight.Value;
        if (delta.UseModBossWeight.HasValue) next.UseModBossWeight = delta.UseModBossWeight.Value;
        if (delta.ModBossWeight.HasValue) next.ModBossWeight = delta.ModBossWeight.Value;
        if (delta.UseModEventWeight.HasValue) next.UseModEventWeight = delta.UseModEventWeight.Value;
        if (delta.ModEventWeight.HasValue) next.ModEventWeight = delta.ModEventWeight.Value;

        return next;
    }

    public static void OnLocalConfigChanged()
    {
        if (_applyingRemote) return;
        SaveConfig();
        if (!IsHostAuthority) return;

        // ★ 房主配置变更后，立即把最新值写进 run snapshot
        var state = RunManager.Instance?.DebugOnlyGetState();
        if (state != null)
            TryWriteRunSnapshotConfig(state);

        var netService = RunManager.Instance?.NetService;
        if (netService == null) return;

        RitsuLibSidecarConfigSyncService.RegisterTopic<GameplayConfigDto, GameplayConfigDelta>(
            TopicId,
            GameplayConfigDto.FromConfig(LoadConfig()),
            CanClientRequest,
            ApplyDelta);

        BroadcastHostState("host_change");
    }

    private static void BroadcastHostState(string reason)
    {
        if (!IsHostAuthority) return;
        var netService = RunManager.Instance?.NetService;
        if (netService == null) return;

        _applyingRemote = true;
        try
        {
            RitsuLibSidecarConfigSyncService.PublishHostState(
                netService, TopicId, netService.NetId, reason);
        }
        finally
        {
            _applyingRemote = false;
        }
    }

    private static void OnTopicChanged(SidecarConfigTopicChangedEvent evt)
    {
        if (evt.Topic != TopicId) return;
        if (_applyingRemote) return;

        var netService = RunManager.Instance?.NetService;
        if (IsHostAuthority && netService != null && evt.ChangedByPeer == netService.NetId)
            return;

        GameplayConfigDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<GameplayConfigDto>(evt.StateJson);
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"[ConfigSync] 反序列化失败: {ex.Message}");
            return;
        }

        if (dto == null) return;

        _applyingRemote = true;
        try
        {
            var cfg = LoadConfig();
            dto.ApplyTo(cfg);
            // 同 TryApplyRunSnapshotConfig：只改内存，不覆盖客机自己的 config.json

            // Sidecar 收到后也写一次 snapshot，保持两端一致
            var state = RunManager.Instance?.DebugOnlyGetState();
            if (state != null)
                RunConfigSlot.Modify(state, data => data.CopyFrom(cfg));

            Entry.Logger.Info($"[ConfigSync] 已应用房主配置 rev={evt.Revision} reason={evt.Reason}");
        }
        finally
        {
            _applyingRemote = false;
        }
    }

    private static CuteSakikoModConfigData LoadConfig()
    {
        var store = RitsuLibFramework.GetDataStore(Entry.ModId);
        return store.Get<CuteSakikoModConfigData>("config");
    }

    private static void SaveConfig()
    {
        var store = RitsuLibFramework.GetDataStore(Entry.ModId);
        store.Save("config");
    }
}