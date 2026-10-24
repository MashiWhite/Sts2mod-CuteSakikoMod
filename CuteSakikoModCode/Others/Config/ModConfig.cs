using CuteSakikoMod.CuteSakikoModCode.Systems;
using STS2RitsuLib;

namespace CuteSakikoMod.CuteSakikoModCode.Others.Config;

/// <summary>按钮触发方式。</summary>
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

    // ===== 按钮点击机制（本地设置，不参与联机同步） =====
    public ButtonClickMode ButtonClickMode { get; set; } = ButtonClickMode.SingleClick;
    public float DoubleClickInterval { get; set; } = 0.3f;   // 秒
    public float LongPressDuration { get; set; } = 0.5f;     // 秒

    // ===== 自定义按钮位置（仅垂直位置，0.0 ~ 1.0） =====
    public float ButtonPositionY { get; set; } = 0.75f; // 默认靠下 75%

    // ===== 游戏性开关（参与联机同步） =====
    public bool EggsCard { get; set; }
    public bool EnableModMonsters { get; set; } = true;
    public bool EnableCustomAncients { get; set; } = true;
    public bool EnableCustomEvents { get; set; } = true;

    /// <summary>
    ///     多人模式下是否缩放压力层数（房主权威开关，默认关闭）。
    /// </summary>
    public bool ScalePressureInMultiplayer { get; set; } = false;

    // ===== 本地设置（不参与联机同步） =====

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