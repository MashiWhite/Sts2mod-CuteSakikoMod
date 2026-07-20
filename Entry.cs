using System.Collections.Generic;
using System.Linq;
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
using MegaCrit.Sts2.Core.Entities.Cards;
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

    // 通配音符类型（仅用于和弦序列识别）
    public static CardType AnyNote;

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

        // 注册通配音符类型
        var cardTypeMinter = new DynamicEnumValueMinter<CardType>();
        AnyNote = cardTypeMinter.Mint("cute_sakiko_mod:any_note");

        CuteSakikoModTelemetry.Register();

        // 1. 注册配置数据存储
        using (RitsuLibFramework.BeginModDataRegistration(ModId))
        {
            var store = RitsuLibFramework.GetDataStore(ModId);
            store.Register("config", "config.json", SaveScope.Global, () => new CuteSakikoModConfigData(), true);
        }

        // 2. 创建绑定（原有 + 新增）
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
    var ancientBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
        ModId, "config",
        model => model.EnableCustomAncients,
        (model, value) => model.EnableCustomAncients = value
    );
    var volumeBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, float>(
        ModId, "config",
        model => model.ModBgmVolume,
        (model, value) => model.ModBgmVolume = value
    );
    var sfxVolumeBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, float>(
        ModId, "config",
        model => model.ModSfxVolume,
        (model, value) => model.ModSfxVolume = value
    );
    var audioBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
        ModId, "config",
        model => model.EnableAudio,
        (model, value) => model.EnableAudio = value
    );
    var customEventBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
        ModId, "config",
        model => model.EnableCustomEvents,
        (model, value) => model.EnableCustomEvents = value
    );

    var i18n = I18n;

    // 3. 注册设置界面：分为“游戏内容”和“音频”两个 Section
    RitsuLibFramework.RegisterModSettings(ModId, page => page
        .WithModDisplayName(ModSettingsText.I18N(i18n, "MOD_SETTINGS.DISPLAY_NAME", "Cute Sakiko Mod"))
        .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.TITLE", "Cute Sakiko Mod Settings"))
        .WithDescription(ModSettingsText.I18N(i18n, "MOD_SETTINGS.DESCRIPTION", "Cute Sakiko Mod Settings"))

        // 游戏内容 Section
        .AddSection("game_content", section => section
            .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.SECTION.GAME_CONTENT", "Game Content"))
            .AddToggle("egg_toggle",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.EGG_TOGGLE.LABEL", "Egg Card"),
                eggBinding,
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.EGG_TOGGLE.DESC", "..."))
            .AddToggle("monster_toggle",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.MONSTER_TOGGLE.LABEL", "Enable Mod Monsters"),
                monsterBinding,
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.MONSTER_TOGGLE.DESC", "..."))
            .AddToggle("ancient_toggle",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.ANCIENT_TOGGLE.LABEL", "Custom Ancient Events"),
                ancientBinding,
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.ANCIENT_TOGGLE.DESC", "Allow custom ancient events to appear naturally."))
            .AddToggle("custom_event_toggle",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.CUSTOM_EVENT_TOGGLE.LABEL", "Custom Events"),
                customEventBinding,
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.CUSTOM_EVENT_TOGGLE.DESC", "Allow custom events to appear naturally.")
            )
        )

        // 音频 Section
        .AddSection("audio", section => section
            .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.SECTION.AUDIO", "Audio"))
            .AddToggle("audio_toggle",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.AUDIO_TOGGLE.LABEL", "Enable Mod Audio"),
                audioBinding,
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.AUDIO_TOGGLE.DESC", "..."))
            .AddSlider("mod_bgm_volume_slider",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_BGM_VOLUME.LABEL", "Mod BGM Volume"),
                volumeBinding,
                0.0f, 1.0f, 0.01f,
                valueFormatter: value => $"{value:P0}",
                description: ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_BGM_VOLUME.DESC", "..."))
            .AddSlider("mod_sfx_volume_slider",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_SFX_VOLUME.LABEL", "Mod SFX Volume"),
                sfxVolumeBinding,
                0.0f, 1.0f, 0.01f,
                valueFormatter: value => $"{value:P0}",
                description: ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_SFX_VOLUME.DESC", "..."))
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

        // ✅ 新增：注册玩家自定义名称数据
        PlayerNameData.Init(runDataStore);

        Log.Debug("Mod initialized!");

        // 7. 事件订阅
        if (RunManager.Instance != null)
            RunManager.Instance.RunStarted += OnRunStarted;
        else
            Logger.Warn("RunManager.Instance is null, RunStarted event not subscribed.");

        // 8. 预加载 VFX
        VFXUtil.PreloadScenes(new List<string> { "res://CuteSakikoMod/scenes/vfx/tokyo_tower.tscn" });

        // 9. 网络消息处理器 + 客户端重连
        RunManager.Instance.RunStarted += _ =>
        {
            var netService = RunManager.Instance.NetService;
            if (netService != null)
            {
                // 原有的 ReloadCountSyncMessage 注册
                netService.RegisterMessageHandler(new MessageHandlerDelegate<ReloadCountSyncMessage>((msg, senderId) =>
                    FlybackManager.OnReloadCountReceived(msg.ReloadCount)));

                // ✅ 新增：NameChangeMessage 注册
                netService.RegisterMessageHandler(new MessageHandlerDelegate<NameChangeMessage>((msg, senderId) =>
                {
                    var runState = NameChangeCmd.GetCurrentRunState();
                    if (runState != null)
                    {
                        PlayerNameData.PlayerNameSlot.Modify(runState, msg.TargetNetId, data =>
                        {
                            data.CustomName = msg.NewName;
                        });
                    }
                    // 刷新所有 UI
                    NameChangeCmd.RefreshAllPlayerNameUI();
                }));
                
                netService.RegisterMessageHandler(new MessageHandlerDelegate<ChordSyncMessage>((msg, senderId) =>
                {
                    // 忽略自己发出的消息（本地已处理）
                    if (msg.PlayerNetId == netService.NetId) return;
    
                    var state = RunManager.Instance.DebugOnlyGetState();
                    if (state == null) return;
                    var player = state.Players.FirstOrDefault(p => p.NetId == msg.PlayerNetId);
                    if (player == null) return;
                    var guitar = player.Relics.OfType<AnonGuitar>().FirstOrDefault();
                    if (guitar == null) return;
    
                    guitar.RestoreChordData(msg.ChordsData, msg.BonusChordsData, "");
                    guitar.SetLearnedChordsFromString(msg.LearnedChordsData);
                }));

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
                "res://CuteSakikoMod/images/others/others/mod_token_card_pool_icon.png"
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
        
        // 离开房间时停止 Mod BGM
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

    private static async void OnRunStarted(RunState state)
    {
        if (!ModConfig.EggsCard) return;
        var me = LocalContext.GetMe(state);
        if (me == null) return;
        if (me.Relics.Any(r => r.Id == ModelDb.Relic<Eggs>().Id)) return;

        var eggs = ModelDb.Relic<Eggs>().ToMutable();
        await RelicCmd.Obtain(eggs, me);
    }

    private static string GetSnakeCaseName(Type type)
    {
        var name = type.Name;
        var snake = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToLower();
        return snake;
    }
}