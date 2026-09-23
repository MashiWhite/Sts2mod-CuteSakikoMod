using System.Reflection;
using System.Text.RegularExpressions;
using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.NetMessage;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Others.Config;
using CuteSakikoMod.CuteSakikoModCode.Others.Telemetry;
using CuteSakikoMod.CuteSakikoModCode.Patches;
using CuteSakikoMod.CuteSakikoModCode.Patches.Skin;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
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

        // 2. 创建绑定
        // 游戏性开关：只有"联机 + 客户端 + 跑局中"才锁定，其他情况（大厅/主菜单/断连后）可自由修改
        // 音频：永远保持本地可改
        var eggBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
            ModId, "config",
            model => model.EggsCard,
            (model, value) =>
            {
                if (GameplayConfigSync.ShouldLockGameplaySettings) return;
                model.EggsCard = value;
                GameplayConfigSync.OnLocalConfigChanged();
            }
        );
        var monsterBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
            ModId, "config",
            model => model.EnableModMonsters,
            (model, value) =>
            {
                if (GameplayConfigSync.ShouldLockGameplaySettings) return;
                model.EnableModMonsters = value;
                GameplayConfigSync.OnLocalConfigChanged();
            }
        );
        var ancientBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
            ModId, "config",
            model => model.EnableCustomAncients,
            (model, value) =>
            {
                if (GameplayConfigSync.ShouldLockGameplaySettings) return;
                model.EnableCustomAncients = value;
                GameplayConfigSync.OnLocalConfigChanged();
            }
        );
        var customEventBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
            ModId, "config",
            model => model.EnableCustomEvents,
            (model, value) =>
            {
                if (GameplayConfigSync.ShouldLockGameplaySettings) return;
                model.EnableCustomEvents = value;
                GameplayConfigSync.OnLocalConfigChanged();
            }
        );
        var volumeBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, double>(
            ModId, "config",
            model => (double)model.ModBgmVolume,
            (model, value) => model.ModBgmVolume = (float)value
        );
        var sfxVolumeBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, double>(
            ModId, "config",
            model => (double)model.ModSfxVolume,
            (model, value) => model.ModSfxVolume = (float)value
        );
        var audioBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
            ModId, "config",
            model => model.EnableAudio,
            (model, value) => model.EnableAudio = value
        );

        var i18n = I18n;

        // 3. 注册设置界面
        RitsuLibFramework.RegisterModSettings(ModId, page => page
            .WithModDisplayName(ModSettingsText.I18N(i18n, "MOD_SETTINGS.DISPLAY_NAME", "Cute Sakiko Mod"))
            .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.TITLE", "Cute Sakiko Mod Settings"))
            .WithDescription(ModSettingsText.I18N(i18n, "MOD_SETTINGS.DESCRIPTION", "Cute Sakiko Mod Settings"))

            // 游戏内容 Section —— 只有跑局中的客户端会灰掉
            .AddSection("game_content", section => section
                .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.SECTION.GAME_CONTENT", "Game Content"))
                .AddToggle("egg_toggle",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.EGG_TOGGLE.LABEL", "Egg Card"),
                    eggBinding,
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.EGG_TOGGLE.DESC", "..."))
                .WithEntryEnabledWhen("egg_toggle", () => !GameplayConfigSync.ShouldLockGameplaySettings)
                .AddToggle("monster_toggle",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.MONSTER_TOGGLE.LABEL", "Enable Mod Monsters"),
                    monsterBinding,
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.MONSTER_TOGGLE.DESC", "..."))
                .WithEntryEnabledWhen("monster_toggle", () => !GameplayConfigSync.ShouldLockGameplaySettings)
                .AddToggle("ancient_toggle",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.ANCIENT_TOGGLE.LABEL", "Custom Ancient Events"),
                    ancientBinding,
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.ANCIENT_TOGGLE.DESC", "Allow custom ancient events to appear naturally."))
                .WithEntryEnabledWhen("ancient_toggle", () => !GameplayConfigSync.ShouldLockGameplaySettings)
                .AddToggle("custom_event_toggle",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.CUSTOM_EVENT_TOGGLE.LABEL", "Custom Events"),
                    customEventBinding,
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.CUSTOM_EVENT_TOGGLE.DESC", "Allow custom events to appear naturally."))
                .WithEntryEnabledWhen("custom_event_toggle", () => !GameplayConfigSync.ShouldLockGameplaySettings)
            )

            // 音频 Section —— 永远是本地设置，所有玩家可改
            .AddSection("audio", section => section
                .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.SECTION.AUDIO", "Audio"))
                .AddToggle("audio_toggle",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.AUDIO_TOGGLE.LABEL", "Enable Mod Audio"),
                    audioBinding,
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.AUDIO_TOGGLE.DESC", "..."))
                .AddSlider("mod_bgm_volume_slider",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_BGM_VOLUME.LABEL", "Mod BGM Volume"),
                    volumeBinding,
                    0.0, 1.0, 0.01,
                    valueFormatter: value => $"{value:P0}",
                    description: ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_BGM_VOLUME.DESC", "..."))
                .AddSlider("mod_sfx_volume_slider",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_SFX_VOLUME.LABEL", "Mod SFX Volume"),
                    sfxVolumeBinding,
                    0.0, 1.0, 0.01,
                    valueFormatter: value => $"{value:P0}",
                    description: ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_SFX_VOLUME.DESC", "..."))
            )
        );

        // 4. Harmony 补丁
        var harmony = new Harmony("White.CuteSakikoMod");
        harmony.PatchAll();
        ModelDbDedupePatches.Apply();

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

        PlayerNameData.Init(runDataStore);

        // ⭐ 6.5 注册 GameplayConfigSync 的 run snapshot 槽位
        //     必须在 GameplayConfigSync.Init() 之前，否则 Init() 会抛异常
        GameplayConfigSync.RunConfigSlot = runDataStore.Register<RunGameplayConfigData>(
            "GameplayConfigSnapshot",
            defaultFactory: () => new RunGameplayConfigData(),
            options: new RunSavedDataOptions
            {
                WritePolicy = RunSavedDataWritePolicy.AlwaysWhenRegistered,
                SyncLobbyOnChange = true,
            });

        // 7. 初始化游戏性配置的房主权威同步
        GameplayConfigSync.Init();

        Log.Debug("Mod initialized!");

        // 8. 预加载 VFX
        VfxUtil.PreloadScenes(new List<string> { "res://CuteSakikoMod/scenes/vfx/tokyo_tower.tscn" });

        // 9. 网络消息处理器 + 房主权威配置同步
        if (RunManager.Instance != null)
        {
            RunManager.Instance.RunStarted += _ =>
            {
                var netService = RunManager.Instance.NetService;

                // 让 GameplayConfigSync 拿到 NetService 做权威判定
                GameplayConfigSync.OnNetServiceReady(netService);

                if (netService != null)
                {
                    netService.RegisterMessageHandler(new MessageHandlerDelegate<ReloadCountSyncMessage>((msg, senderId) =>
                        FlybackManager.OnReloadCountReceived(msg.ReloadCount)));

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
                        NameChangeCmd.RefreshAllPlayerNameUI();
                    }));

                    netService.RegisterMessageHandler(new MessageHandlerDelegate<ChordSyncMessage>((msg, senderId) =>
                    {
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

                    // ⭐ EggsGrantMessage handler
                    netService.RegisterMessageHandler(new MessageHandlerDelegate<EggsGrantMessage>(async (msg, senderId) =>
                    {
                        try
                        {
                            var state = RunManager.Instance.DebugOnlyGetState();
                            if (state == null) return;

                            var player = state.Players.FirstOrDefault(p => p.NetId == msg.TargetPlayerNetId);
                            if (player == null)
                            {
                                Logger.Warn($"[ConfigSync] EggsGrantMessage: 找不到 player {msg.TargetPlayerNetId}");
                                return;
                            }
                            if (player.Relics.Any(r => r.Id == ModelDb.Relic<Eggs>().Id))
                            {
                                Logger.Info($"[ConfigSync] player {msg.TargetPlayerNetId} 已有 Eggs，跳过");
                                return;
                            }

                            Logger.Info($"[ConfigSync] 客户端本地给 player {msg.TargetPlayerNetId} 补发 Eggs");
                            var eggs = ModelDb.Relic<Eggs>().ToMutable();
                            await RelicCmd.Obtain(eggs, player);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn($"[ConfigSync] EggsGrantMessage 处理失败: {ex}");
                        }
                    }));

                    if (netService is NetHostGameService hostService)
                        hostService.ClientConnected += peerId => { FlybackManager.SyncReloadCountIfHost(); };
                }
            };
        }
        else
        {
            Logger.Warn("RunManager.Instance is null, RunStarted event not subscribed.");
        }

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

        // ⭐ 10. Eggs 遗物发放：章节切换完成时由房主统一发（每个玩家一条消息）
        RitsuLibFramework.SubscribeLifecycle<ActEnteredEvent>(async evt =>
        {
            try
            {
                if (!ModConfig.EggsCard) return;

                var netService = RunManager.Instance.NetService;
                var isHostOrSingle = netService?.Type is NetGameType.Singleplayer or NetGameType.Host;
                if (!isHostOrSingle) return;

                Logger.Info($"[ConfigSync] ActEnteredEvent: 检查并发放 Eggs (act={evt.CurrentActIndex})");

                foreach (var player in evt.RunState.Players)
                {
                    if (player.Relics.Any(r => r.Id == ModelDb.Relic<Eggs>().Id)) continue;

                    Logger.Info($"[ConfigSync] 房主本地给 player {player.NetId} 发放 Eggs");
                    var eggs = ModelDb.Relic<Eggs>().ToMutable();
                    await RelicCmd.Obtain(eggs, player);

                    // 每个玩家单独广播一条消息
                    if (netService != null && netService.Type == NetGameType.Host)
                    {
                        netService.SendMessage(new EggsGrantMessage
                        {
                            TargetPlayerNetId = player.NetId
                        });
                        Logger.Info($"[ConfigSync] 广播 EggsGrantMessage: netId={player.NetId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"[ConfigSync] ActEnteredEvent 发放 Eggs 失败: {ex}");
            }
        });

        // ⭐ 11. 战斗开始兜底：只校验不发放，避免与战斗状态机冲突
        RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(evt =>
        {
            if (!ModConfig.EggsCard) return;

            var netService = RunManager.Instance.NetService;
            var isHostOrSingle = netService?.Type is NetGameType.Singleplayer or NetGameType.Host;
            if (!isHostOrSingle) return;

            foreach (var player in evt.RunState.Players)
            {
                if (!player.Relics.Any(r => r.Id == ModelDb.Relic<Eggs>().Id))
                    Logger.Warn($"[ConfigSync] 战斗开始时 player {player.NetId} 没有 Eggs（ActEnteredEvent 发放失败？）");
            }
        });

        // 离开房间时停止 Mod BGM
        RitsuLibFramework.SubscribeLifecycle<RoomExitedEvent>(_ =>
        {
            AudioManager.StopMusic();
        });

        // 12. 战斗结束时自动停止 Mod BGM
        if (RunManager.Instance != null)
        {
            RunManager.Instance.RunStarted += _ =>
            {
                if (CombatManager.Instance != null)
                    CombatManager.Instance.CombatEnded += _ => AudioManager.StopMusic();
            };
        }
        ObPopupHelper.PreloadButtonScene();

        // 触发静态构造
        _ = ChordNoteSystem.MaxStoredChords;
    }

    private static string GetSnakeCaseName(Type type)
    {
        var name = type.Name;
        var snake = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToLower();
        return snake;
    }
}