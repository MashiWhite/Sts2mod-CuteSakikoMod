namespace CuteSakikoMod.CuteSakikoModCode.Others.Config;

public sealed class GameplayConfigDto
{
    public bool EggsCard { get; set; }
    public bool EnableModMonsters { get; set; } = true;
    public bool EnableCustomAncients { get; set; } = true;
    public bool EnableCustomEvents { get; set; } = true;
    public bool ScalePressureInMultiplayer { get; set; }
    
    public bool UseModAncientWeight { get; set; } = true;
    public float ModAncientWeight { get; set; } = 0.5f;
    public bool UseModBossWeight { get; set; } = true;
    public float ModBossWeight { get; set; } = 0.5f;
    public bool UseModEventWeight { get; set; } = true;
    public float ModEventWeight { get; set; } = 0.8f;

    public static GameplayConfigDto FromConfig(CuteSakikoModConfigData cfg)
    {
        return new GameplayConfigDto
        {
            EggsCard = cfg.EggsCard,
            EnableModMonsters = cfg.EnableModMonsters,
            EnableCustomAncients = cfg.EnableCustomAncients,
            EnableCustomEvents = cfg.EnableCustomEvents,
            ScalePressureInMultiplayer = cfg.ScalePressureInMultiplayer,
            UseModAncientWeight = cfg.UseModAncientWeight,
            ModAncientWeight = cfg.ModAncientWeight,
            UseModBossWeight = cfg.UseModBossWeight,
            ModBossWeight = cfg.ModBossWeight,
            UseModEventWeight = cfg.UseModEventWeight,
            ModEventWeight = cfg.ModEventWeight
        };
    }

    public void ApplyTo(CuteSakikoModConfigData cfg)
    {
        cfg.EggsCard = EggsCard;
        cfg.EnableModMonsters = EnableModMonsters;
        cfg.EnableCustomAncients = EnableCustomAncients;
        cfg.EnableCustomEvents = EnableCustomEvents;
        cfg.ScalePressureInMultiplayer = ScalePressureInMultiplayer;
        cfg.UseModAncientWeight = UseModAncientWeight;
        cfg.ModAncientWeight = ModAncientWeight;
        cfg.UseModBossWeight = UseModBossWeight;
        cfg.ModBossWeight = ModBossWeight;
        cfg.UseModEventWeight = UseModEventWeight;
        cfg.ModEventWeight = ModEventWeight;
    }

    public GameplayConfigDto Clone()
    {
        return new GameplayConfigDto
        {
            EggsCard = EggsCard,
            EnableModMonsters = EnableModMonsters,
            EnableCustomAncients = EnableCustomAncients,
            EnableCustomEvents = EnableCustomEvents,
            ScalePressureInMultiplayer = ScalePressureInMultiplayer,
            UseModAncientWeight = UseModAncientWeight,
            ModAncientWeight = ModAncientWeight,
            UseModBossWeight = UseModBossWeight,
            ModBossWeight = ModBossWeight,
            UseModEventWeight = UseModEventWeight,
            ModEventWeight = ModEventWeight
        };
    }
}

public sealed class GameplayConfigDelta
{
    public bool? EggsCard { get; set; }
    public bool? EnableModMonsters { get; set; }
    public bool? EnableCustomAncients { get; set; }
    public bool? EnableCustomEvents { get; set; }
    public bool? ScalePressureInMultiplayer { get; set; }
    
    public bool? UseModAncientWeight { get; set; }
    public float? ModAncientWeight { get; set; }
    public bool? UseModBossWeight { get; set; }
    public float? ModBossWeight { get; set; }
    public bool? UseModEventWeight { get; set; }
    public float? ModEventWeight { get; set; }
}

