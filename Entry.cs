using System.Reflection;
using System.Text.RegularExpressions;
using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.NetMessage;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
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
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop;
using STS2RitsuLib.RunData;
using STS2RitsuLib.Settings;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace CuteSakikoMod;

[ModInitializer(nameof(Init))]
public class Entry
{
    public const string ModId = "CuteSakikoMod";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    // 芭菲充能数据槽位
    public static PlayerRunSavedData<PlayerParfaitData> ParfaitChargesSlot = null!;

    // I18N 多语言实例（从文件系统加载 localization 文件夹下的 JSON）
    private static I18N? _i18n;
    private static I18N I18n => _i18n ??= new I18N(
        instanceName: ModId,
        fsFolders: new[] { $"res://{ModId}/localization" }
    );

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
        var monsterBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
            ModId, "config",
            model => model.EnableModMonsters,
            (model, value) => model.EnableModMonsters = value
        );

        // 3. 注册设置界面（多语言支持）
        var i18n = I18n;
        RitsuLibFramework.RegisterModSettings(ModId, page => page
            .WithModDisplayName(ModSettingsText.I18N(i18n, "MOD_SETTINGS.DISPLAY_NAME", "Cute Sakiko Mod"))
            .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.TITLE", "Cute Sakiko Mod Settings"))
            .AddSection("general", section => section
                .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.SECTION.GENERAL", "General"))
                .AddToggle("egg_toggle",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.EGG_TOGGLE.LABEL", "Egg Card"),
                    eggBinding,
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.EGG_TOGGLE.DESC", "Automatically obtain the Egg Relic at the start of the game"))
                .AddToggle("monster_toggle",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.MONSTER_TOGGLE.LABEL", "Enable Mod Monsters"),
                    monsterBinding,
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.MONSTER_TOGGLE.DESC", "If enabled, custom monsters and encounters from the mod will appear naturally"))
            )
        );

        // 4. Harmony 补丁
        var harmony = new Harmony("White.CuteSakikoMod");
        harmony.PatchAll();

        // 5. 提前注册自定义牌堆
        MemoryCardPile.Register(ModId);
        ForgetCardPile.Register(ModId);

        // 6. 注册 RunSavedData 槽位
        var runDataStore = RunSavedDataStore.For(ModId);
        FlybackManager.RunDataSlot = runDataStore.Register<RunFlybackData>("FlybackRunData",
            options: new RunSavedDataOptions
            {
                WritePolicy = RunSavedDataWritePolicy.WhenSet,  // 只要修改就保存
                SyncLobbyOnChange = false  // 大厅阶段不需要同步
            });
        FlybackManager.PlayerDataSlot = runDataStore.RegisterPerPlayer<PlayerFlybackData>("FlybackPlayerData",
            options: new RunSavedDataOptions
                { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault, SyncLobbyOnChange = true });
        Eggs.PlayerEggsSlot = runDataStore.RegisterPerPlayer<PlayerEggsData>("EggsSelected",
            options: new RunSavedDataOptions
                { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault, SyncLobbyOnChange = true });

        // 注册芭菲充能数据槽位
        ParfaitChargesSlot = runDataStore.RegisterPerPlayer(
            "ParfaitCharges",
            defaultFactory: () => new PlayerParfaitData(),
            options: new RunSavedDataOptions
            {
                WritePolicy = RunSavedDataWritePolicy.WhenSet,
                SyncLobbyOnChange = true
            });

        Log.Debug("Mod initialized!");

        // 7. 事件订阅
        if (RunManager.Instance != null)
            RunManager.Instance.RunStarted += OnRunStarted;
        else
            Logger.Warn("RunManager.Instance is null, RunStarted event not subscribed.");

        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(TimeWatch));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(AnonGuitar));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(FlashAnonGuitar));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(MatchaParfait));
        SavedPropertiesTypeCache.InjectTypeIntoCache(typeof(BigMatchaParfait));
        
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
        // 不再需要手动重置，因为 ReloadCount 已经保存在 RunFlybackData 中
        // 新游戏会自动创建新实例，计数从0开始

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

// 配置数据类（会持久化到 config.json）
public class CuteSakikoModConfigData
{
    public bool 彩蛋卡 { get; set; }
    public bool EnableModMonsters { get; set; } = true;
    public bool Config2 { get; set; } = false;
    public bool Config3 { get; set; } = false;
}

// 统一配置访问入口（确保只存在一处）
public static class ModConfig
{
    private static CuteSakikoModConfigData? _cached;
    private static readonly object _lock = new();

    public static bool 彩蛋卡 => Load().彩蛋卡;
    public static bool EnableModMonsters => Load().EnableModMonsters;
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