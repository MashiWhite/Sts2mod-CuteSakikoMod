using CuteSakikoMod.CuteSakikoModCode.Systems;
using STS2RitsuLib;

namespace CuteSakikoMod.CuteSakikoModCode.Others.Config;

// 配置数据类（持久化到 config.json）
// CuteSakikoModCode/Others/Config/CuteSakikoModConfigData.cs
public class CuteSakikoModConfigData
{
    private float _modBgmVolume = 0.40f;
    private float _modSfxVolume = 0.40f;

    public bool EggsCard { get; set; }
    public bool EnableModMonsters { get; set; } = true;

    private bool _enableAudio = true;

    public bool EnableAudio
    {
        get => _enableAudio;
        set
        {
            if (_enableAudio != value)
            {
                _enableAudio = value;
                if (!value)
                {
                    AudioManager.StopMusic();
                }
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

    public float ModSfxVolume
    {
        get => _modSfxVolume;
        set => _modSfxVolume = value;
    }

    public bool EnableCustomAncients { get; set; } = true;
    public bool EnableCustomEvents { get; set; } = true;

    /// <summary>
    /// 是否启用模组表情贴纸替换（本地视觉设置，不参与联机同步）。
    /// </summary>
    public bool EnableReactionReplacement { get; set; } = true;

    /// <summary>
    /// 反应轮盘整体放大倍数（1.0 = 原版大小）。
    /// </summary>
    public float ReactionWheelScale { get; set; } = 1.0f;

    /// <summary>
    /// 飘出表情的放大倍数（3.0 = 原版三倍）。
    /// </summary>
    public float ReactionEmoteScale { get; set; } = 3.0f;
}

// 统一配置访问入口
public static class ModConfig
{
    private static CuteSakikoModConfigData? _cached;
    private static readonly object _lock = new();

    public static bool EnableAudio => Load().EnableAudio;
    public static bool EggsCard => Load().EggsCard;
    public static bool EnableModMonsters => Load().EnableModMonsters;
    public static float ModBgmVolume => Load().ModBgmVolume;
    public static float ModSfxVolume => Load().ModSfxVolume;

    public static bool EnableCustomAncients => Load().EnableCustomAncients;
    public static bool EnableCustomEvents => Load().EnableCustomEvents;

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