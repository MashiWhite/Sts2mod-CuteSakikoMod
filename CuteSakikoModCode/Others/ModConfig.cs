using CuteSakikoMod.CuteSakikoModCode.Systems;
using STS2RitsuLib;

namespace CuteSakikoMod.CuteSakikoModCode.Others;

// 配置数据类（持久化到 config.json）
public class CuteSakikoModConfigData
{
    private float _modBgmVolume = 0.8f;

    public bool EggsCard { get; set; }
    public bool EnableModMonsters { get; set; } = true;

    public float ModBgmVolume
    {
        get => _modBgmVolume;
        set
        {
            _modBgmVolume = value;
            // 实时更新正在播放的音乐音量
            AudioManager.RefreshMusicVolume();
        }
    }
}

// 统一配置访问入口
public static class ModConfig
{
    private static CuteSakikoModConfigData? _cached;
    private static readonly object _lock = new();

    public static bool EggsCard => Load().EggsCard;
    public static bool EnableModMonsters => Load().EnableModMonsters;
    public static float ModBgmVolume => Load().ModBgmVolume;

    private static CuteSakikoModConfigData Load()
    {
        if (_cached != null) return _cached;
        lock (_lock)
        {
            if (_cached != null) return _cached;
            // 使用添加的 using 后，RitsuLibFramework 可直接访问
            var store = RitsuLibFramework.GetDataStore(Entry.ModId);
            _cached = store.Get<CuteSakikoModConfigData>("config");
            return _cached;
        }
    }
}