using System.Reflection;
using System.Text.RegularExpressions;
using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.NetMessage;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop;
using STS2RitsuLib.RunData;
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
            store.Register("config", "config.json", SaveScope.Global, () => new CuteSakikoModConfigData(), true);
        }

        // 2. 创建绑定
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
                .AddToggle("egg_toggle", ModSettingsText.Literal("彩蛋卡"), eggBinding,
                    ModSettingsText.Literal("启用后游戏开始时自动获得彩蛋遗物"))
            )
        );

        // 4. Harmony 补丁
        var harmony = new Harmony("White.CuteSakikoMod");
        harmony.PatchAll();

        // ★ 5. 提前注册自定义牌堆（必须在牌堆注册表冻结前完成）
        MemoryCardPile.Register(ModId);
        ForgetCardPile.Register(ModId);

        // 6. 注册 RunSavedData 槽位
        var runDataStore = RunSavedDataStore.For(ModId);
        FlybackManager.RunDataSlot = runDataStore.Register<RunFlybackData>("FlybackRunData",
            options: new RunSavedDataOptions
                { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault, SyncLobbyOnChange = false });
        FlybackManager.PlayerDataSlot = runDataStore.RegisterPerPlayer<PlayerFlybackData>("FlybackPlayerData",
            options: new RunSavedDataOptions
                { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault, SyncLobbyOnChange = true });
        Eggs.PlayerEggsSlot = runDataStore.RegisterPerPlayer<PlayerEggsData>("EggsSelected",
            options: new RunSavedDataOptions
                { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault, SyncLobbyOnChange = true });

        Log.Debug("Mod initialized!");

        // 7. 事件订阅
        if (RunManager.Instance != null)
            RunManager.Instance.RunStarted += OnRunStarted;
        else
            Logger.Warn("RunManager.Instance is null, RunStarted event not subscribed.");

        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(TimeWatch));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(AnonGuitar));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(FlashAnonGuitar));

        // 8. 预加载 VFX
        VFXUtil.PreloadScenes(new List<string> { "res://CuteSakikoMod/scenes/vfx/tokyo_tower.tscn" });

        // 9. 注册网络消息处理器（仅 ReloadCount）+ 监听客户端重连
        RunManager.Instance.RunStarted += _ =>
        {
            var netService = RunManager.Instance.NetService;
            if (netService != null)
            {
                netService.RegisterMessageHandler(new MessageHandlerDelegate<ReloadCountSyncMessage>((msg, senderId) =>
                    FlybackManager.OnReloadCountReceived(msg.ReloadCount)));
                if (netService is NetHostGameService hostService)
                    hostService.ClientConnected += peerId => { FlybackManager.SyncReloadCountIfHost(); };
            }
        };

        ModContentRegistry.For(ModId)
            .RegisterCardLibraryCompendiumSharedPoolFilter<CuteSakikoModCardPool>(
                "cute_sakiko_mod_card_pool",
                "res://CuteSakikoMod/images/others/others/mod_card_pool_icon.png"
            );

        RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(evt =>
        {
            if (!ModConfig.彩蛋卡) return;
            var netService = RunManager.Instance.NetService;
            var isHostOrSingle = netService?.Type == NetGameType.Singleplayer || netService?.Type == NetGameType.Host;
            if (!isHostOrSingle) return;
            foreach (var player in evt.RunState.Players)
            {
                if (player.Relics.Any(r => r.Id == ModelDb.Relic<Eggs>().Id)) continue;
                var eggs = ModelDb.Relic<Eggs>().ToMutable();
                _ = RelicCmd.Obtain(eggs, player);
            }
        });
    }

    private static void OnRunStarted(RunState state)
    {
        if (!ModConfig.彩蛋卡) return;

        var me = LocalContext.GetMe(state);
        if (me == null) return;
        if (me.Relics.Any(r => r.Id == ModelDb.Relic<Eggs>().Id)) return;

        _ = Task.Run(async () =>
        {
            var eggs = ModelDb.Relic<Eggs>().ToMutable();
            await RelicCmd.Obtain(eggs, me);
        });
    }

    private static string GetSnakeCaseName(Type type)
    {
        var name = type.Name;
        var snake = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToLower();
        return snake;
    }
}

// 配置数据类保持不变
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