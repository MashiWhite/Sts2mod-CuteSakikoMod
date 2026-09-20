namespace CuteSakikoMod.CuteSakikoModCode.Others.Config;

/// <summary>
/// 随 run snapshot 同步的游戏性配置。
/// 与 GameplayConfigDto 字段一致，但作为 RunSavedData 使用。
/// </summary>
public sealed class RunGameplayConfigData
{
    public bool EggsCard { get; set; }
    public bool EnableModMonsters { get; set; } = true;
    public bool EnableCustomAncients { get; set; } = true;
    public bool EnableCustomEvents { get; set; } = true;

    public void CopyFrom(CuteSakikoModConfigData cfg)
    {
        EggsCard = cfg.EggsCard;
        EnableModMonsters = cfg.EnableModMonsters;
        EnableCustomAncients = cfg.EnableCustomAncients;
        EnableCustomEvents = cfg.EnableCustomEvents;
    }

    public void ApplyTo(CuteSakikoModConfigData cfg)
    {
        cfg.EggsCard = EggsCard;
        cfg.EnableModMonsters = EnableModMonsters;
        cfg.EnableCustomAncients = EnableCustomAncients;
        cfg.EnableCustomEvents = EnableCustomEvents;
    }
}