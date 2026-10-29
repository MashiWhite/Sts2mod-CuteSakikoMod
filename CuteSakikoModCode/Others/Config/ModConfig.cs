using CuteSakikoMod.CuteSakikoModCode.Systems;
using STS2RitsuLib;

namespace CuteSakikoMod.CuteSakikoModCode.Others.Config;

public enum ButtonClickMode
{
    SingleClick,
    DoubleClick,
    LongPress
}

public class CuteSakikoModConfigData
{
    private bool _enableAudio = true;
    private float _modBgmVolume = 0.40f;

    // ===== 按钮点击机制（本地设置） =====
    public ButtonClickMode ButtonClickMode { get; set; } = ButtonClickMode.SingleClick;
    public float DoubleClickInterval { get; set; } = 0.3f;
    public float LongPressDuration { get; set; } = 0.5f;
    public float ButtonPositionY { get; set; } = 0.75f;

    // ===== 游戏性开关（参与联机同步） =====
    public bool EggsCard { get; set; }
    public bool EnableModMonsters { get; set; } = true;
    public bool EnableCustomAncients { get; set; } = true;
    public bool EnableCustomEvents { get; set; } = true;
    public bool ScalePressureInMultiplayer { get; set; } = false;

    // ===== 权重设置（参与联机同步） =====
    /// <summary>
    ///     权重系统总开关，**默认关闭**。
    ///     关闭时 RoomGenerationWeightPatch 直接 return，不消耗任何 RNG、不改任何 rooms，
    ///     保证不影响原版/其他 mod 的生成结果（用户要求默认关闭）。
    /// </summary>
    public bool UseModWeight { get; set; } = false;

    public bool UseModAncientWeight { get; set; } = true;
    public float ModAncientWeight { get; set; } = 0.5f;

    public bool UseModBossWeight { get; set; } = true;
    public float ModBossWeight { get; set; } = 0.5f;

    public bool UseModEventWeight { get; set; } = true;
    public float ModEventWeight { get; set; } = 0.8f;

    // ===== 本地设置 =====
    public bool EnableAudio
    {
        get => _enableAudio;
        set
        {
            if (_enableAudio != value)
            {
                _enableAudio = value;
                if (!value) AudioManager.StopMusic();
            }
        }
    }

    public float ModBgmVolume
    {
        get => _modBgmVolume;
        set
        {
            _modBgmVolume = value;
            AudioManager.RefreshMusicVolume();
        }
    }

    public float ModSfxVolume { get; set; } = 0.40f;
    public bool EnableReactionReplacement { get; set; } = true;
    public float ReactionWheelScale { get; set; } = 1.0f;
    public float ReactionEmoteScale { get; set; } = 3.0f;
}

public static class ModConfig
{
    private static CuteSakikoModConfigData? _cached;
    private static readonly object _lock = new();

    public static ButtonClickMode ButtonClickMode => Load().ButtonClickMode;
    public static float DoubleClickInterval => Load().DoubleClickInterval;
    public static float LongPressDuration => Load().LongPressDuration;
    public static float ButtonPositionY => Load().ButtonPositionY;
    public static bool EnableAudio => Load().EnableAudio;
    public static bool EggsCard => Load().EggsCard;
    public static bool EnableModMonsters => Load().EnableModMonsters;
    public static float ModBgmVolume => Load().ModBgmVolume;
    public static float ModSfxVolume => Load().ModSfxVolume;
    public static bool EnableCustomAncients => Load().EnableCustomAncients;
    public static bool EnableCustomEvents => Load().EnableCustomEvents;
    public static bool ScalePressureInMultiplayer => Load().ScalePressureInMultiplayer;
    public static bool EnableReactionReplacement => Load().EnableReactionReplacement;
    public static float ReactionWheelScale => Load().ReactionWheelScale;
    public static float ReactionEmoteScale => Load().ReactionEmoteScale;
    
    // 新增权重读取
    public static bool UseModWeight => Load().UseModWeight;
    public static bool UseModAncientWeight => Load().UseModAncientWeight;
    public static float ModAncientWeight => Load().ModAncientWeight;
    public static bool UseModBossWeight => Load().UseModBossWeight;
    public static float ModBossWeight => Load().ModBossWeight;
    public static bool UseModEventWeight => Load().UseModEventWeight;
    public static float ModEventWeight => Load().ModEventWeight;

    private static CuteSakikoModConfigData Load()
    {
        if (_cached != null) return _cached;
        lock (_lock)
        {
            if (_cached != null) return _cached;
            var store = RitsuLibFramework.GetDataStore(Entry.ModId);
            _cached = store.Get<CuteSakikoModConfigData>("config");
            return _cached;
        }
    }
}