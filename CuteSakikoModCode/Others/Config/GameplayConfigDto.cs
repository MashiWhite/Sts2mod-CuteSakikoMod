namespace CuteSakikoMod.CuteSakikoModCode.Others.Config;

/// <summary>
/// 联机同步用的游戏性配置快照（Sidecar 用）。
/// 只包含游戏性开关，音频等本地偏好不在此列。
/// </summary>
public sealed class GameplayConfigDto
{
    public bool EggsCard { get; set; }
    public bool EnableModMonsters { get; set; } = true;
    public bool EnableCustomAncients { get; set; } = true;
    public bool EnableCustomEvents { get; set; } = true;

    public static GameplayConfigDto FromConfig(CuteSakikoModConfigData cfg) => new()
    {
        EggsCard = cfg.EggsCard,
        EnableModMonsters = cfg.EnableModMonsters,
        EnableCustomAncients = cfg.EnableCustomAncients,
        EnableCustomEvents = cfg.EnableCustomEvents,
    };

    public void ApplyTo(CuteSakikoModConfigData cfg)
    {
        cfg.EggsCard = EggsCard;
        cfg.EnableModMonsters = EnableModMonsters;
        cfg.EnableCustomAncients = EnableCustomAncients;
        cfg.EnableCustomEvents = EnableCustomEvents;
    }

    public GameplayConfigDto Clone() => new()
    {
        EggsCard = EggsCard,
        EnableModMonsters = EnableModMonsters,
        EnableCustomAncients = EnableCustomAncients,
        EnableCustomEvents = EnableCustomEvents,
    };
}

/// <summary>增量更新：null 表示该字段未变化。</summary>
public sealed class GameplayConfigDelta
{
    public bool? EggsCard { get; set; }
    public bool? EnableModMonsters { get; set; }
    public bool? EnableCustomAncients { get; set; }
    public bool? EnableCustomEvents { get; set; }
}
