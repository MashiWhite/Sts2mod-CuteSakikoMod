using CuteSakikoMod.CuteSakikoModCode.Systems;
using STS2RitsuLib;

namespace CuteSakikoMod.CuteSakikoModCode.Others;

// 配置数据类（持久化到 config.json）
// CuteSakikoModCode/Others/CuteSakikoModConfigData.cs
public class CuteSakikoModConfigData
{
    private float _modBgmVolume = 0.40f;
    private float _modSfxVolume = 0.40f;   // 新增

    public bool EggsCard { get; set; }
    public bool EnableModMonsters { get; set; } = true;

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
        set => _modSfxVolume = value;   // 即时生效，无需额外操作
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
    public static float ModSfxVolume => Load().ModSfxVolume;   // 新增

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