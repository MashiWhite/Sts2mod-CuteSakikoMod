using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Config;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Event;
using Godot;
using Godot.Bridge;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Others;

[ModInitializer("Init")]
public class Entry
{
    public static void Init()
    {
        var harmony = new Harmony("White.CuteSakikoMod");
        harmony.PatchAll();

        ModConfigRegistry.Register("CuteSakikoMod", new ModConfig());

        ScriptManagerBridge.LookupScriptsInAssembly(typeof(Entry).Assembly);
        Log.Debug("Mod initialized!");

        // 预加载所有场景（递归）
        PreloadScenes();

        // 异步预加载其他资源
        Task.Run(PreloadAssets);

        RunManager.Instance.RunStarted += OnRunStarted;
    }

    private static void PreloadScenes()
    {
        string scenesDir = "res://CuteSakikoMod/scenes/";
        PreloadScenesRecursive(scenesDir);
        Log.Info("Scenes preloaded.");
    }

    private static void PreloadScenesRecursive(string dirPath)
    {
        using var dir = DirAccess.Open(dirPath);
        if (dir == null) return;
        dir.ListDirBegin();
        string fileName;
        while ((fileName = dir.GetNext()) != "")
        {
            if (fileName == "." || fileName == "..") continue;
            string fullPath = dirPath + fileName;
            if (dir.CurrentIsDir())
            {
                PreloadScenesRecursive(fullPath + "/");
            }
            else if (fileName.EndsWith(".tscn"))
            {
                if (ResourceLoader.Exists(fullPath))
                    PreloadManager.Cache.GetScene(fullPath);
            }
        }
        dir.ListDirEnd();
    }

    private static async Task PreloadAssets()
    {
        try
        {
            Log.Info("Preloading CuteSakikoMod assets...");

            // 预加载所有自定义卡牌的肖像
            var cardTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsSubclassOf(typeof(CardModel)) && !t.IsAbstract);
            foreach (var type in cardTypes)
            {
                try
                {
                    var instance = Activator.CreateInstance(type) as CardModel;
                    if (instance?.PortraitPath != null)
                    {
                        PreloadManager.Cache.GetTexture2D(instance.PortraitPath);
                    }
                }
                catch { }
            }

            // 预加载所有自定义能力的图标
            var powerTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsSubclassOf(typeof(PowerModel)) && !t.IsAbstract);
            foreach (var type in powerTypes)
            {
                try
                {
                    var instance = Activator.CreateInstance(type) as PowerModel;
                    if (instance is CustomPowerModel customPower && customPower.CustomPackedIconPath != null)
                    {
                        PreloadManager.Cache.GetTexture2D(customPower.CustomPackedIconPath);
                    }
                }
                catch { }
            }

            // 预加载所有自定义遗物的图标（仅公开属性）
            var relicTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsSubclassOf(typeof(RelicModel)) && !t.IsAbstract);
            foreach (var type in relicTypes)
            {
                try
                {
                    var instance = Activator.CreateInstance(type) as RelicModel;
                    if (instance is CustomRelicModel customRelic && customRelic.PackedIconPath != null)
                    {
                        PreloadManager.Cache.GetTexture2D(customRelic.PackedIconPath);
                    }
                }
                catch { }
            }

            Log.Info("Preloading completed.");
        }
        catch (Exception ex)
        {
            Log.Error($"Preload failed: {ex.Message}");
        }
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

    public class ModConfig : SimpleModConfig
    {
        public static bool 彩蛋卡 { get; set; } = false;
        public static bool Config2 { get; set; } = false;
        public static bool Config3 { get; set; } = false;
    }
}