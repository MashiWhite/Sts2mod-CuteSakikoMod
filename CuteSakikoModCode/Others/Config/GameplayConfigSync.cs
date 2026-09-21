using System.Text.Json;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Networking.Sidecar;
using STS2RitsuLib.RunData;

namespace CuteSakikoMod.CuteSakikoModCode.Others.Config;

/// <summary>
/// 游戏性配置的房主权威同步。
/// 只同步游戏性开关（彩蛋卡 / 怪物 / 古代事件 / 普通事件），音频设置保持本地。
/// </summary>
public static class GameplayConfigSync
{
    public const string TopicId = "cute_sakiko_gameplay";

    /// <summary>本机是否为权威（单机或房主）。</summary>
    public static bool IsHostAuthority { get; private set; }

    /// <summary>run snapshot 槽位；必须在 Init() 之前由 Entry 注册。</summary>
    public static RunSavedData<RunGameplayConfigData> RunConfigSlot = null!;

    // 防止远端快照写回本地配置时触发回环广播
    private static bool _applyingRemote;

    private static IDisposable? _handshakeSub;
    private static IDisposable? _topicChangedSub;
    private static IDisposable? _sessionBoundSub;
    private static IDisposable? _sessionUnboundSub;
    private static bool _runStartedSubscribed;

    /// <summary>
    /// Entry.Init() 里调用一次，注册 topic、订阅事件。
    /// 调用前必须先注册 RunConfigSlot。
    /// </summary>
    public static void Init()
    {
        if (RunConfigSlot == null)
            throw new InvalidOperationException(
                "[GameplayConfigSync] RunConfigSlot 必须在 Init() 之前注册。");

        UpdateAuthority(RunManager.Instance?.NetService);

        // 两端都必须注册 topic，否则 Sidecar 不会挂上快照/请求处理器
        RitsuLibSidecarConfigSyncService.RegisterTopic<GameplayConfigDto, GameplayConfigDelta>(
            TopicId,
            GameplayConfigDto.FromConfig(LoadConfig()),
            CanClientRequest,
            ApplyDelta);

        _topicChangedSub ??= RitsuLibSidecarEvents.OnConfigTopicChanged(OnTopicChanged);

        // ⭐ 订阅会话绑定/解绑，实时更新权威状态
        _sessionBoundSub ??= RitsuLibSidecarEvents.OnSessionBound(evt =>
        {
            UpdateAuthority(evt.NetService);
            Entry.Logger.Info(
                $"[ConfigSync] SessionBound: type={evt.NetService.Type} IsHostAuthority={IsHostAuthority}");
        });

        _sessionUnboundSub ??= RitsuLibSidecarEvents.OnSessionUnbound(_ =>
        {
            // 会话解绑 = 离开联机房间，回退到本地/单机权威
            UpdateAuthority(null);
            Entry.Logger.Info($"[ConfigSync] SessionUnbound: IsHostAuthority={IsHostAuthority}");
        });

        // 订阅官方 RunStartedEvent
        if (!_runStartedSubscribed)
        {
            _runStartedSubscribed = true;
            RitsuLibFramework.SubscribeLifecycle<RunStartedEvent>(OnRunStarted);
        }
    }

    /// <summary>原版 RunStarted 之后调用，用于客户端读取 run snapshot。</summary>
    public static void OnNetServiceReady(INetGameService? netService)
    {
        UpdateAuthority(netService);

        if (IsHostAuthority)
        {
            EnsureHandshakeSubscription();
            // snapshot 写入由 RunStartedEvent 处理
            return;
        }

        // 客户端：立即从 run snapshot 应用房主配置
        TryApplyRunSnapshotConfig();
    }

    private static void UpdateAuthority(INetGameService? netService)
    {
        // netService 为 null 视为单机（本地权威）
        IsHostAuthority = netService == null
            || netService.Type is NetGameType.Singleplayer or NetGameType.Host;
    }

    private static void EnsureHandshakeSubscription()
    {
        _handshakeSub ??= RitsuLibSidecarEvents.OnHandshakeCompleted(
            _ => BroadcastHostState("handshake"));
    }

    // ========== RunStartedEvent（房主写 snapshot） ==========

    private static void OnRunStarted(RunStartedEvent evt)
    {
        if (!IsHostAuthority) return;

        Entry.Logger.Info($"[ConfigSync] RunStartedEvent: IsMultiplayer={evt.IsMultiplayer}");

        TryWriteRunSnapshotConfig(evt.RunState);
        BroadcastHostState("run_started");
    }

    /// <summary>房主把当前真实配置写入 run snapshot。</summary>
    private static void TryWriteRunSnapshotConfig(RunState state)
    {
        try
        {
            var cfg = LoadConfig();
            Entry.Logger.Info(
                $"[ConfigSync] 房主写入 run snapshot: Eggs={cfg.EggsCard} Monsters={cfg.EnableModMonsters} " +
                $"Ancients={cfg.EnableCustomAncients} Events={cfg.EnableCustomEvents}");

            RunConfigSlot.Modify(state, data => data.CopyFrom(cfg));
        }
        catch (Exception ex)
        {
            Entry.Logger.Warn($"[ConfigSync] 房主写 snapshot 失败: {ex.Message}");
        }
    }

    // ========== 客户端读 run snapshot ==========

    /// <summary>客户端在 RunStarted 时调用，从 RunSavedData 读房主配置。</summary>
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
                $"Ancients={data.EnableCustomAncients} Events={data.EnableCustomEvents}");

            _applyingRemote = true;
            try
            {
                var cfg = LoadConfig();
                data.ApplyTo(cfg);
                SaveConfig();
                Entry.Logger.Info(
                    $"[ConfigSync] 从 run snapshot 应用房主配置: Eggs={cfg.EggsCard} Monsters={cfg.EnableModMonsters}");
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

    /// <summary>只允许房主自己通过请求路径更新；客户端一律拒绝。</summary>
    private static bool CanClientRequest(ulong sender, GameplayConfigDelta delta)
    {
        var netService = RunManager.Instance?.NetService;
        if (netService == null) return true;   // 单机
        return sender == netService.NetId;     // 仅房主
    }

    private static GameplayConfigDto ApplyDelta(GameplayConfigDto current, GameplayConfigDelta delta)
    {
        var next = current.Clone();
        if (delta.EggsCard.HasValue) next.EggsCard = delta.EggsCard.Value;
        if (delta.EnableModMonsters.HasValue) next.EnableModMonsters = delta.EnableModMonsters.Value;
        if (delta.EnableCustomAncients.HasValue) next.EnableCustomAncients = delta.EnableCustomAncients.Value;
        if (delta.EnableCustomEvents.HasValue) next.EnableCustomEvents = delta.EnableCustomEvents.Value;
        return next;
    }

    /// <summary>
    /// 本地游戏性配置改变后调用。房主会刷新 topic 并广播；客户端只落盘。
    /// 音频等本地设置不要调用此方法。
    /// </summary>
    public static void OnLocalConfigChanged()
    {
        if (_applyingRemote) return;
        SaveConfig();
        if (!IsHostAuthority) return;

        var netService = RunManager.Instance?.NetService;
        if (netService == null) return;

        // 用本地最新配置刷新 Topics[topic].StateJson（PublishHostState 只广播缓存）。
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

        // 房主自己触发的 PublishHostState 也会走到这里，跳过
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
            SaveConfig();
            Entry.Logger.Info($"[ConfigSync] 已应用房主配置 rev={evt.Revision} reason={evt.Reason}");
        }
        finally
        {
            _applyingRemote = false;
        }
    }

    // ========== 工具 ==========

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