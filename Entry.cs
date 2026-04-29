using System.Reflection;
using System.Text.RegularExpressions;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Event;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Interop;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils.Persistence;
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

        // 6. 异步预加载所有资源（不等待，避免阻塞启动）
        _ = Task.Run(PreloadAllAssetsAsync);
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

    // ==================== 预缓存资源 ====================
    private static async Task PreloadAllAssetsAsync()
    {
        try
        {
            Logger.Info("Starting async asset preloading...");

            var assembly = Assembly.GetExecutingAssembly();
            var allTypes = assembly.GetTypes();

            // 预加载卡片立绘
            var cardTypes = allTypes.Where(t => t.IsSubclassOf(typeof(ModCardTemplate)) && !t.IsAbstract);
            foreach (var type in cardTypes)
            {
                var snakeName = GetSnakeCaseName(type);
                var path = $"res://{ModId}/images/cards/{snakeName}.png";
                // 直接调用 GetTexture2D，如果文件缺失会打印警告但不崩溃
                PreloadManager.Cache.GetTexture2D(path);
            }

            // 预加载能力图标
            var powerTypes = allTypes.Where(t => t.IsSubclassOf(typeof(ModPowerTemplate)) && !t.IsAbstract);
            foreach (var type in powerTypes)
            {
                var snakeName = GetSnakeCaseName(type);
                PreloadManager.Cache.GetTexture2D($"res://{ModId}/images/powers/{snakeName}.png");
                PreloadManager.Cache.GetTexture2D($"res://{ModId}/images/powers/big/{snakeName}.png");
            }

            // 预加载遗物图标
            var relicTypes = allTypes.Where(t => t.IsSubclassOf(typeof(ModRelicTemplate)) && !t.IsAbstract);
            foreach (var type in relicTypes)
            {
                var snakeName = GetSnakeCaseName(type);
                PreloadManager.Cache.GetTexture2D($"res://{ModId}/images/relics/{snakeName}.png");
                PreloadManager.Cache.GetTexture2D($"res://{ModId}/images/relics/{snakeName}_outline.png");
                PreloadManager.Cache.GetTexture2D($"res://{ModId}/images/relics/big/{snakeName}.png");
            }

            // 预加载药水图标
            var potionTypes = allTypes.Where(t => t.IsSubclassOf(typeof(ModPotionTemplate)) && !t.IsAbstract);
            foreach (var type in potionTypes)
            {
                var snakeName = GetSnakeCaseName(type);
                PreloadManager.Cache.GetTexture2D($"res://{ModId}/images/potions/{snakeName}.png");
                PreloadManager.Cache.GetTexture2D($"res://{ModId}/images/potions/{snakeName}_outline.png");
            }

            // 预加载场景
            var scenesDir = $"res://{ModId}/scenes/";
            using var dir = DirAccess.Open(scenesDir);
            if (dir != null)
            {
                dir.ListDirBegin();
                string fileName;
                while ((fileName = dir.GetNext()) != "")
                {
                    if (fileName == "." || fileName == "..") continue;
                    var fullPath = scenesDir + fileName;
                    if (dir.CurrentIsDir()) continue;
                    if (fileName.EndsWith(".tscn")) PreloadManager.Cache.GetScene(fullPath);
                }

                dir.ListDirEnd();
            }

            Logger.Info("Async asset preloading completed.");
        }
        catch (Exception ex)
        {
            Logger.Error($"PreloadAllAssetsAsync failed: {ex.Message}");
        }
    }

    // 工具方法：PascalCase → snake_case
    private static string GetSnakeCaseName(Type type)
    {
        var name = type.Name;
        var snake = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToLower();
        return snake;
    }
}

// 配置数据类（如果还没有单独文件，可以放在这里，但建议独立文件）
public class CuteSakikoModConfigData
{
    public bool 彩蛋卡 { get; set; }
    public bool Config2 { get; set; } = false;
    public bool Config3 { get; set; } = false;
}

// 静态配置访问器（如果还没有单独文件，可以放在这里）
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