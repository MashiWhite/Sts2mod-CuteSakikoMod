using System.Reflection;
using System.Text.RegularExpressions;
using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.NetMessage;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Others.Telemetry;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
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
using STS2RitsuLib.Telemetry;
using STS2RitsuLib.Utils;
using STS2RitsuLib.Utils.Persistence;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace CuteSakikoMod;

[ModInitializer(nameof(Init))]
public class Entry
{
    public const string ModId = "CuteSakikoMod";
    public static readonly Logger Logger = RitsuLibFramework.CreateLogger(ModId);

    public static PlayerRunSavedData<PlayerParfaitData> ParfaitChargesSlot = null!;

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

        CuteSakikoModTelemetry.Register();

        // 1. 注册配置数据存储
        using (RitsuLibFramework.BeginModDataRegistration(ModId))
        {
            var store = RitsuLibFramework.GetDataStore(ModId);
            store.Register("config", "config.json", SaveScope.Global, () => new CuteSakikoModConfigData(), true);
        }

        // 2. 创建绑定
        var eggBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
            ModId, "config",
            model => model.EggsCard,
            (model, value) => model.EggsCard = value
        );
        var monsterBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
            ModId, "config",
            model => model.EnableModMonsters,
            (model, value) => model.EnableModMonsters = value
        );
        var volumeBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, float>(
            ModId, "config",
            model => model.ModBgmVolume,
            (model, value) => model.ModBgmVolume = value
        );

        // 3. 注册设置界面
        var i18n = I18n;
        RitsuLibFramework.RegisterModSettings(ModId, page => page
            .WithModDisplayName(ModSettingsText.I18N(i18n, "MOD_SETTINGS.DISPLAY_NAME", "Cute Sakiko Mod"))
            .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.TITLE", "Cute Sakiko Mod Settings"))
            .WithDescription(ModSettingsText.I18N(i18n, "MOD_SETTINGS.DESCRIPTION", "Cute Sakiko Mod Settings"))
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
                .AddSlider("mod_bgm_volume_slider",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_BGM_VOLUME.LABEL", "Mod BGM Volume"),
                    volumeBinding,
                    0.0f, 1.0f, 0.01f,
                    valueFormatter: value => $"{value:P0}",
                    description: ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_BGM_VOLUME.DESC", "Controls the volume of mod-specific background music."))
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
                WritePolicy = RunSavedDataWritePolicy.WhenSet,
                SyncLobbyOnChange = false
            });
        FlybackManager.PlayerDataSlot = runDataStore.RegisterPerPlayer<PlayerFlybackData>("FlybackPlayerData",
            options: new RunSavedDataOptions
            { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault, SyncLobbyOnChange = true });
        Eggs.PlayerEggsSlot = runDataStore.RegisterPerPlayer<PlayerEggsData>("EggsSelected",
            options: new RunSavedDataOptions
            { WritePolicy = RunSavedDataWritePolicy.WhenNonDefault, SyncLobbyOnChange = true });

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

        // 9. 网络消息处理器 + 客户端重连
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
        ModContentRegistry.For(ModId)
            .RegisterCardLibraryCompendiumSharedPoolFilter<CuteSakikoTokenCardPool>(
                "cute_sakiko_token_card_pool",
                "res://CuteSakikoMod/images/others/others/mod_card_pool_icon.png"
            );

        RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(evt =>
        {
            if (!ModConfig.EggsCard) return;
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
        
        // 离开房间时停止 Mod BGM（覆盖战斗结束、保存退出、放弃等所有情况）
        RitsuLibFramework.SubscribeLifecycle<RoomExitedEvent>(_ =>
        {
            AudioManager.StopMusic();
        });

        // 10. 战斗结束时自动停止 Mod BGM
        RunManager.Instance.RunStarted += _ =>
        {
            if (CombatManager.Instance != null)
                CombatManager.Instance.CombatEnded += _ => AudioManager.StopMusic();
        };
    }

    private static void OnRunStarted(RunState state)
    {
        if (!ModConfig.EggsCard) return;
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