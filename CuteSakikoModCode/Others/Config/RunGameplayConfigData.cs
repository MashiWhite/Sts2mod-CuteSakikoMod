namespace CuteSakikoMod.CuteSakikoModCode.Others.Config;

public sealed class RunGameplayConfigData
{
    public bool EggsCard { get; set; }
    public bool EnableModMonsters { get; set; } = true;
    public bool EnableCustomAncients { get; set; } = true;
    public bool EnableCustomEvents { get; set; } = true;
    public bool ScalePressureInMultiplayer { get; set; }
    
    /// <summary>权重系统总开关，默认关闭。</summary>
    public bool UseModWeight { get; set; }
    public bool UseModAncientWeight { get; set; } = true;
    public float ModAncientWeight { get; set; } = 0.5f;
    public bool UseModBossWeight { get; set; } = true;
    public float ModBossWeight { get; set; } = 0.5f;
    public bool UseModEventWeight { get; set; } = true;
    public float ModEventWeight { get; set; } = 0.8f;

    public void CopyFrom(CuteSakikoModConfigData cfg)
    {
        EggsCard = cfg.EggsCard;
        EnableModMonsters = cfg.EnableModMonsters;
        EnableCustomAncients = cfg.EnableCustomAncients;
        EnableCustomEvents = cfg.EnableCustomEvents;
        ScalePressureInMultiplayer = cfg.ScalePressureInMultiplayer;
        UseModWeight = cfg.UseModWeight;
        UseModAncientWeight = cfg.UseModAncientWeight;
        ModAncientWeight = cfg.ModAncientWeight;
        UseModBossWeight = cfg.UseModBossWeight;
        ModBossWeight = cfg.ModBossWeight;
        UseModEventWeight = cfg.UseModEventWeight;
        ModEventWeight = cfg.ModEventWeight;
    }

    public void ApplyTo(CuteSakikoModConfigData cfg)
    {
        cfg.EggsCard = EggsCard;
        cfg.EnableModMonsters = EnableModMonsters;
        cfg.EnableCustomAncients = EnableCustomAncients;
        cfg.EnableCustomEvents = EnableCustomEvents;
        cfg.ScalePressureInMultiplayer = ScalePressureInMultiplayer;
        cfg.UseModWeight = UseModWeight;
        cfg.UseModAncientWeight = UseModAncientWeight;
        cfg.ModAncientWeight = ModAncientWeight;
        cfg.UseModBossWeight = UseModBossWeight;
        cfg.ModBossWeight = ModBossWeight;
        cfg.UseModEventWeight = UseModEventWeight;
        cfg.ModEventWeight = ModEventWeight;
    }
}