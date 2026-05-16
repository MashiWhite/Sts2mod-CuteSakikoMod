using System.Reflection;
using System.Text.RegularExpressions;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Event;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;
using Godot;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace CuteSakikoMod;

[ModInitializer(nameof(Init))]
public class Entry
{
    public const string ModId = "CuteSakikoMod";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

        // 1. 注册配置数据存储
        using (RitsuLibFramework.BeginModDataRegistration(ModId))
        {
            var store = RitsuLibFramework.GetDataStore(ModId);
            store.Register<CuteSakikoModConfigData>(
                "config",
                "config.json",
                SaveScope.Global,
                () => new CuteSakikoModConfigData(),
                true
            );
        }

        // 2. 创建绑定（用于设置界面）
        var eggBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
            ModId, "config",
            model => model.彩蛋卡,
            (model, value) => model.彩蛋卡 = value
        );

        // 3. 注册设置界面
        RitsuLibFramework.RegisterModSettings(ModId, page => page
            .WithModDisplayName(ModSettingsText.Literal("Cute Sakiko Mod"))
            .WithTitle(ModSettingsText.Literal("Cute Sakiko Mod 设置"))
            .AddSection("general", section => section
                .WithTitle(ModSettingsText.Literal("通用"))
                .AddToggle(
                    "egg_toggle",
                    ModSettingsText.Literal("彩蛋卡"),
                    eggBinding,
                    ModSettingsText.Literal("启用后游戏开始时自动获得彩蛋遗物")
                )
            )
        );

        // 4. Harmony 补丁
        var harmony = new Harmony("White.CuteSakikoMod");
        harmony.PatchAll();

        Log.Debug("Mod initialized!");

        // 5. 事件订阅
        if (RunManager.Instance != null)
            RunManager.Instance.RunStarted += OnRunStarted;
        else
            Logger.Warn("RunManager.Instance is null, RunStarted event not subscribed.");
        
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(TimeWatch));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(AnonGuitar));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(FlashAnonGuitar));

        // ===== 预加载自定义特效 =====
        GD.Load<PackedScene>("res://CuteSakikoMod/scenes/vfx/tokyo_tower.tscn");
        
    }

    private static void OnRunStarted(RunState state)
    {
        if (!ModConfig.彩蛋卡) return;

        var player = state.Players.FirstOrDefault(p => p.Creature.IsPlayer);
        if (player == null) return;
        if (player.Relics.Any(r => r.Id == ModelDb.Relic<Eggs>().Id)) return;

        _ = Task.Run(async () =>
        {
            var eggs = ModelDb.Relic<Eggs>().ToMutable();
            await RelicCmd.Obtain(eggs, player);
        });
    }

    private static string GetSnakeCaseName(Type type)
    {
        var name = type.Name;
        var snake = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToLower();
        return snake;
    }
}

// 以下两个辅助类保持不变（如果独立文件已存在可不复制）
public class CuteSakikoModConfigData
{
    public bool 彩蛋卡 { get; set; }
    public bool Config2 { get; set; } = false;
    public bool Config3 { get; set; } = false;
}

public static class ModConfig
{
    private static CuteSakikoModConfigData? _cached;
    private static readonly object _lock = new();

    public static bool 彩蛋卡 => Load().彩蛋卡;
    public static bool Config2 => Load().Config2;
    public static bool Config3 => Load().Config3;

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