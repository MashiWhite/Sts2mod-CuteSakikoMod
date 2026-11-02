using System.Reflection;
using System.Text.RegularExpressions;
using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Monsters.Weak;
using CuteSakikoMod.CuteSakikoModCode.NetMessage;
using CuteSakikoMod.CuteSakikoModCode.Nodes.SakiButton;
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
using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Core;
using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Definitions;
using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Networking;
using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Ui;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib;
using STS2RitsuLib.Content;
using STS2RitsuLib.Interop;
using STS2RitsuLib.RunData;
using STS2RitsuLib.Scaffolding.Godot.NodeAttachments;
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

    public static CardType AnyNote;

    private static I18N? _i18n;

    private static I18N I18n => _i18n ??= new I18N(
        ModId,
        new[] { $"res://{ModId}/localization" }
    );

    public static void Init()
    {
        var assembly = Assembly.GetExecutingAssembly();
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);

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
        // 事件分类子开关：需要 mod 角色 / 普通事件
        var characterLockedEventBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
            ModId, "config",
            model => model.EnableCharacterLockedEvents,
            (model, value) =>
            {
                if (GameplayConfigSync.ShouldLockGameplaySettings) return;
                model.EnableCharacterLockedEvents = value;
                GameplayConfigSync.OnLocalConfigChanged();
            }
        );
        var plainEventBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
            ModId, "config",
            model => model.EnablePlainEvents,
            (model, value) =>
            {
                if (GameplayConfigSync.ShouldLockGameplaySettings) return;
                model.EnablePlainEvents = value;
                GameplayConfigSync.OnLocalConfigChanged();
            }
        );
        var scalePressureBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
            ModId, "config",
            model => model.ScalePressureInMultiplayer,
            (model, value) =>
            {
                if (GameplayConfigSync.ShouldLockGameplaySettings) return;
                model.ScalePressureInMultiplayer = value;
                GameplayConfigSync.OnLocalConfigChanged();
            }
        );
        // 权重系统总开关（默认关闭）——关闭时不干预任何房间生成
        var modWeightBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
            ModId, "config",
            model => model.UseModWeight,
            (model, value) =>
            {
                if (GameplayConfigSync.ShouldLockGameplaySettings) return;
                model.UseModWeight = value;
                GameplayConfigSync.OnLocalConfigChanged();
            }
        );
        var volumeBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, double>(
            ModId, "config",
            model => model.ModBgmVolume,
            (model, value) => model.ModBgmVolume = (float)value
        );
        var sfxVolumeBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, double>(
            ModId, "config",
            model => model.ModSfxVolume,
            (model, value) => model.ModSfxVolume = (float)value
        );
        var audioBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
            ModId, "config",
            model => model.EnableAudio,
            (model, value) => model.EnableAudio = value
        );

        var reactionBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, bool>(
            ModId, "config",
            model => model.EnableReactionReplacement,
            (model, value) => model.EnableReactionReplacement = value
        );
        var wheelScaleBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, double>(
            ModId, "config",
            model => model.ReactionWheelScale,
            (model, value) => model.ReactionWheelScale = (float)value
        );
        var emoteScaleBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, double>(
            ModId, "config",
            model => model.ReactionEmoteScale,
            (model, value) => model.ReactionEmoteScale = (float)value
        );
        
        var buttonPosYBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, double>(
            ModId, "config",
            model => model.ButtonPositionY,
            (model, value) => 
            {
                model.ButtonPositionY = (float)value;
                // ★ 新增：通知已存在的按钮实时刷新位置
                ModActionButton.NotifyPositionChanged();
            }
        );
        // 绑定逻辑 (在创建 reactionBinding 之前)
        var modAncientWeightBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, double>(
            ModId, "config", model => model.ModAncientWeight,
            (model, value) => { if (!GameplayConfigSync.ShouldLockGameplaySettings) { model.ModAncientWeight = (float)value; GameplayConfigSync.OnLocalConfigChanged(); } }
        );
        var modBossWeightBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, double>(
            ModId, "config", model => model.ModBossWeight,
            (model, value) => { if (!GameplayConfigSync.ShouldLockGameplaySettings) { model.ModBossWeight = (float)value; GameplayConfigSync.OnLocalConfigChanged(); } }
        );
        var modEventWeightBinding = ModSettingsBindings.Global<CuteSakikoModConfigData, double>(
            ModId, "config", model => model.ModEventWeight,
            (model, value) => { if (!GameplayConfigSync.ShouldLockGameplaySettings) { model.ModEventWeight = (float)value; GameplayConfigSync.OnLocalConfigChanged(); } }
        );

        var i18n = I18n;

        // 3. 注册设置界面
        //    ── 2026-11-02 重构：主页面只保留「分类导航」，具体设置全部下沉到 5 个子页
        //       （游戏内容 / 事件 / 视听 / 按钮 / 音频）。
        //       子页由下方 4 段 RegisterModSettings(...).AsChildOf(ModId) 注册，
        //       主页面用 AddSubpage("xxx_entry", 标签, "子页 id", 按钮文字, 说明) 挂上去。
        //       ⚠️ AddSubpage 的第 3 个参数必须是已注册子页的 pageId，写错会点不开。
        RitsuLibFramework.RegisterModSettings(ModId, page => page
            .WithModDisplayName(ModSettingsText.I18N(i18n, "MOD_SETTINGS.DISPLAY_NAME", "Cute Sakiko Mod"))
            .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.TITLE", "Cute Sakiko Mod Settings"))
            .WithDescription(ModSettingsText.I18N(i18n, "MOD_SETTINGS.DESCRIPTION", "Cute Sakiko Mod Settings"))
            .AddSection("nav", section => section
                .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.SECTION.NAV", "设置分类"))
                .AddSubpage("game_content_entry",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.GAME_CONTENT_PAGE.LABEL", "游戏内容"),
                    "game_content_page",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.GAME_CONTENT_PAGE.BUTTON", "打开"),
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.GAME_CONTENT_PAGE.DESC",
                        "彩蛋卡、怪物、先古之民、联机压力缩放，以及 mod 内容权重。"))
                .AddSubpage("event_settings_entry",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.EVENT_SETTINGS.LABEL", "事件设置"),
                    "event_settings",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.EVENT_SETTINGS.BUTTON", "打开"),
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.EVENT_SETTINGS.DESC",
                        "总开关与分类开关：需要 mod 角色的事件 / 普通事件。"))
                .AddSubpage("visual_entry",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.VISUAL_PAGE.LABEL", "视听表现"),
                    "visual_page",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.VISUAL_PAGE.BUTTON", "打开"),
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.VISUAL_PAGE.DESC",
                        "贴图表情替换、转盘与表情尺寸。"))
                .AddSubpage("button_click_entry",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.BUTTON_PAGE.LABEL", "按钮设置"),
                    "button_click_page",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.BUTTON_PAGE.BUTTON", "打开"),
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.BUTTON_PAGE.DESC",
                        "按钮点击方式、双击间隔、长按时长与垂直位置。"))
                .AddSubpage("audio_entry",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.AUDIO_PAGE.LABEL", "音频"),
                    "audio_page",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.AUDIO_PAGE.BUTTON", "打开"),
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.AUDIO_PAGE.DESC",
                        "mod 音效与背景音乐音量。"))
            ),
            // ★ 必须显式给 pageId：不传时默认用 ModId 作 pageId，
            //   而子页若不传也会撞上同一个 key，导致「后注册的整页替换前面的」（见 配置存档联机.txt §6.4）
            ModId
        );

        // 3.1 子页：游戏内容（彩蛋 / 怪物 / 先古 / 压力缩放 / 内容权重）
        RitsuLibFramework.RegisterModSettings(ModId, page => page
            .AsChildOf(ModId)
            .WithSortOrder(10)
            .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.GAME_CONTENT_PAGE.TITLE", "游戏内容"))
            .WithDescription(ModSettingsText.I18N(i18n, "MOD_SETTINGS.GAME_CONTENT_PAGE.TITLE_DESC",
                "控制 mod 的卡牌彩蛋、怪物、先古之民、联机压力缩放与内容权重。"))
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
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.ANCIENT_TOGGLE.DESC",
                        "Allow custom ancient events to appear naturally."))
                .WithEntryEnabledWhen("ancient_toggle", () => !GameplayConfigSync.ShouldLockGameplaySettings)
                .AddToggle("scale_pressure_toggle",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.SCALE_PRESSURE.LABEL", "Multiplayer Pressure Scaling"),
                    scalePressureBinding,
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.SCALE_PRESSURE.DESC",
                        "Scale Pressure stacks applied to enemies based on player count in multiplayer. Host-authoritative."))
                .WithEntryEnabledWhen("scale_pressure_toggle", () => !GameplayConfigSync.ShouldLockGameplaySettings)
                .AddToggle("mod_weight_toggle",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_WEIGHT.LABEL", "启用 mod 内容权重设置"),
                    modWeightBinding,
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_WEIGHT.DESC",
                        "总开关，默认关闭。关闭时完全不影响房间生成；打开后下面的三项权重才会生效。"))
                .WithEntryEnabledWhen("mod_weight_toggle", () => !GameplayConfigSync.ShouldLockGameplaySettings)
                .AddSlider("mod_ancient_weight_slider",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_ANCIENT_WEIGHT.LABEL", "自定义先古之民出现权重"),
                    modAncientWeightBinding, 0.0, 1.0, 0.05, value => $"{value:P0}",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_ANCIENT_WEIGHT.DESC", "决定先古之民生成时，由你的 mod 替换原版先古的概率。"))
                .WithEntryEnabledWhen("mod_ancient_weight_slider", () => !GameplayConfigSync.ShouldLockGameplaySettings && ModConfig.UseModWeight)

                .AddSlider("mod_boss_weight_slider",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_BOSS_WEIGHT.LABEL", "自定义 Boss 出现权重"),
                    modBossWeightBinding, 0.0, 1.0, 0.05, value => $"{value:P0}",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_BOSS_WEIGHT.DESC", "决定关底 Boss 生成时，由你的 mod 替换原版 Boss 的概率。"))
                .WithEntryEnabledWhen("mod_boss_weight_slider", () => !GameplayConfigSync.ShouldLockGameplaySettings && ModConfig.UseModWeight)

                .AddSlider("mod_event_weight_slider",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_EVENT_WEIGHT.LABEL", "自定义事件占比"),
                    modEventWeightBinding, 0.0, 1.0, 0.05, value => $"{value:P0}",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_EVENT_WEIGHT.DESC", "生成事件池时，优先抽取 mod 事件的比例。"))
                .WithEntryEnabledWhen("mod_event_weight_slider", () => !GameplayConfigSync.ShouldLockGameplaySettings && ModConfig.UseModWeight)
            ), "game_content_page");

        // 3.2 子页：视听表现（表情贴图替换 / 转盘与表情尺寸）
        RitsuLibFramework.RegisterModSettings(ModId, page => page
            .AsChildOf(ModId)
            .WithSortOrder(20)
            .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.VISUAL_PAGE.TITLE", "视听表现"))
            .WithDescription(ModSettingsText.I18N(i18n, "MOD_SETTINGS.VISUAL_PAGE.TITLE_DESC",
                "控制 mod 表情贴图替换，以及反应转盘与弹出表情的尺寸。"))
            .AddSection("reaction", section => section
            .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.SECTION.REACTION", "Reaction Stickers"))
            .AddToggle("reaction_replacement_toggle",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.REACTION_REPLACEMENT.LABEL", "Replace Stickers"),
                reactionBinding,
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.REACTION_REPLACEMENT.DESC",
                    "Replace vanilla reaction stickers with mod character portraits."))
            .AddSlider("reaction_wheel_scale_slider",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.REACTION_WHEEL_SCALE.LABEL", "Wheel Scale"),
                wheelScaleBinding,
                0.5, 3.0, 0.1,
                value => $"{value:F1}x",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.REACTION_WHEEL_SCALE.DESC",
                    "Scale of the reaction wheel (1.0 = vanilla size)."))
            .WithEntryEnabledWhen("reaction_wheel_scale_slider", () => ModConfig.EnableReactionReplacement)
            .AddSlider("reaction_emote_scale_slider",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.REACTION_EMOTE_SCALE.LABEL", "Emote Scale"),
                emoteScaleBinding,
                1.0, 6.0, 0.5,
                value => $"{value:F1}x",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.REACTION_EMOTE_SCALE.DESC",
                    "Scale of the popped-out reaction emote (3.0 = vanilla 3x)."))
            .WithEntryEnabledWhen("reaction_emote_scale_slider", () => ModConfig.EnableReactionReplacement)
            ), "visual_page");

        // 3.3 子页：按钮设置（点击方式 / 双击间隔 / 长按 / 垂直位置）
        RitsuLibFramework.RegisterModSettings(ModId, page => page
            .AsChildOf(ModId)
            .WithSortOrder(30)
            .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.BUTTON_PAGE.TITLE", "按钮设置"))
            .WithDescription(ModSettingsText.I18N(i18n, "MOD_SETTINGS.BUTTON_PAGE.TITLE_DESC",
                "控制 mod 按钮的点击方式、双击判定间隔、长按时长与垂直位置。"))
            .AddSection("button_click", section => section
            .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.SECTION.BUTTON_CLICK", "Button Click"))
            .AddChoice("button_click_mode",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.BUTTON_CLICK.MODE.LABEL", "Click Mode"),
                new ModSettingsValueBinding<CuteSakikoModConfigData, ButtonClickMode>(
                    ModId, "config", SaveScope.Global,
                    model => model.ButtonClickMode,
                    (model, value) => model.ButtonClickMode = value),
                new[]
                {
                    new ModSettingsChoiceOption<ButtonClickMode>(
                        ButtonClickMode.SingleClick,
                        ModSettingsText.I18N(i18n, "MOD_SETTINGS.BUTTON_CLICK.MODE.SINGLE", "Single Click")),
                    new ModSettingsChoiceOption<ButtonClickMode>(
                        ButtonClickMode.DoubleClick,
                        ModSettingsText.I18N(i18n, "MOD_SETTINGS.BUTTON_CLICK.MODE.DOUBLE", "Double Click")),
                    new ModSettingsChoiceOption<ButtonClickMode>(
                        ButtonClickMode.LongPress,
                        ModSettingsText.I18N(i18n, "MOD_SETTINGS.BUTTON_CLICK.MODE.LONG_PRESS", "Long Press"))
                },
                presentation: ModSettingsChoicePresentation.Dropdown)
            .AddSlider("double_click_interval",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.BUTTON_CLICK.DOUBLE_INTERVAL.LABEL", "Double Click Interval"),
                ModSettingsBindings.Global<CuteSakikoModConfigData, double>(
                    ModId, "config",
                    model => model.DoubleClickInterval,
                    (model, value) => model.DoubleClickInterval = (float)value),
                0.1, 1.0, 0.05,
                value => $"{value:F2}s",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.BUTTON_CLICK.DOUBLE_INTERVAL.DESC",
                    "Maximum time between two clicks."))
            .AddSlider("long_press_duration",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.BUTTON_CLICK.LONG_PRESS.LABEL", "Long Press Duration"),
                ModSettingsBindings.Global<CuteSakikoModConfigData, double>(
                    ModId, "config",
                    model => model.LongPressDuration,
                    (model, value) => model.LongPressDuration = (float)value),
                0.2, 2.0, 0.05,
                value => $"{value:F2}s",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.BUTTON_CLICK.LONG_PRESS.DESC",
                    "How long to hold before confirming."))
            .AddSlider("button_pos_y",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.BUTTON_CLICK.POS_Y.LABEL", "Vertical Position (Y)"),
                buttonPosYBinding,
                0.1, 0.9, 0.01,
                value => $"{value:P0}",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.BUTTON_CLICK.POS_Y.DESC", 
                    "Adjust the vertical position of mod buttons. (X-axis is fixed to right edge)"))
            ), "button_click_page");

        // 3.4 子页：音频（mod 音效 / 背景音乐音量）
        RitsuLibFramework.RegisterModSettings(ModId, page => page
            .AsChildOf(ModId)
            .WithSortOrder(40)
            .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.AUDIO_PAGE.TITLE", "音频"))
            .WithDescription(ModSettingsText.I18N(i18n, "MOD_SETTINGS.AUDIO_PAGE.TITLE_DESC",
                "控制 mod 音效与背景音乐的音量。"))
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
                value => $"{value:P0}",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_BGM_VOLUME.DESC", "..."))
            .AddSlider("mod_sfx_volume_slider",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_SFX_VOLUME.LABEL", "Mod SFX Volume"),
                sfxVolumeBinding,
                0.0, 1.0, 0.01,
                value => $"{value:P0}",
                ModSettingsText.I18N(i18n, "MOD_SETTINGS.MOD_SFX_VOLUME.DESC", "..."))
            ), "audio_page");

        // 3.5 事件设置子页面：总开关 + 两个分类开关
        RitsuLibFramework.RegisterModSettings(ModId, page => page
            .AsChildOf(ModId)
            .WithSortOrder(15)
            .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.EVENT_SETTINGS.TITLE", "事件设置"))
            .WithDescription(ModSettingsText.I18N(i18n, "MOD_SETTINGS.EVENT_SETTINGS.TITLE_DESC",
                "配置 Mod 事件的总体开关，以及按“是否需要 mod 角色”划分的分类开关。"))
            .AddSection("event_master", section => section
                .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.SECTION.EVENT_MASTER", "总开关"))
                .AddToggle("custom_event_toggle",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.CUSTOM_EVENT_TOGGLE.LABEL", "事件总开关"),
                    customEventBinding,
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.CUSTOM_EVENT_TOGGLE.DESC",
                        "关闭后，本 mod 的所有事件都不会出现；下面的分类开关也随之失效。"))
                .WithEntryEnabledWhen("custom_event_toggle", () => !GameplayConfigSync.ShouldLockGameplaySettings)
            )
            .AddSection("event_types", section => section
                .WithTitle(ModSettingsText.I18N(i18n, "MOD_SETTINGS.SECTION.EVENT_TYPES", "分类开关"))
                .WithEnabledWhen(() => ModConfig.EnableCustomEvents)
                .AddToggle("character_locked_event_toggle",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.CHARACTER_LOCKED_EVENT.LABEL", "需要 mod 角色的事件"),
                    characterLockedEventBinding,
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.CHARACTER_LOCKED_EVENT.DESC",
                        "仅在拥有对应 mod 角色（爱音 / 祥子 / 乐奈）时才能解锁全部选项的事件。单人游玩时会因缺少角色而出现锁定选项。"))
                .WithEntryEnabledWhen("character_locked_event_toggle", () => !GameplayConfigSync.ShouldLockGameplaySettings)
                .AddToggle("plain_event_toggle",
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.PLAIN_EVENT.LABEL", "普通事件"),
                    plainEventBinding,
                    ModSettingsText.I18N(i18n, "MOD_SETTINGS.PLAIN_EVENT.DESC",
                        "不需要 mod 角色即可完整体验的事件，单人游玩也能拿到全部内容。"))
                .WithEntryEnabledWhen("plain_event_toggle", () => !GameplayConfigSync.ShouldLockGameplaySettings)
            ),
            "event_settings"
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
            () => new PlayerParfaitData(),
            new RunSavedDataOptions
            {
                WritePolicy = RunSavedDataWritePolicy.WhenSet,
                SyncLobbyOnChange = true
            });

        PlayerNameData.Init(runDataStore);

        GameplayConfigSync.RunConfigSlot = runDataStore.Register(
            "GameplayConfigSnapshot",
            () => new RunGameplayConfigData(),
            new RunSavedDataOptions
            {
                WritePolicy = RunSavedDataWritePolicy.AlwaysWhenRegistered,
                SyncLobbyOnChange = true
            });

        // 7. 初始化游戏性配置的房主权威同步
        GameplayConfigSync.Init();

        Log.Debug("Mod initialized!");

        // 8. 预加载 VFX
        VfxUtil.PreloadScenes(new List<string> { Xiaoyin.BunVfxPath,"res://CuteSakikoMod/scenes/vfx/tokyo_tower.tscn" });

        // ===== 皮肤系统 =====
        // 8.1 数据槽 + 同步服务
        SkinDataStore.Register(ModId);
        SkinSyncService.Init();
        SkinCharacterStatsRefreshPatch.Init();

        // 8.2 注册皮肤表
        CharacterSkinRegistry.Register(new SakiSkinRegistry());
        CharacterSkinRegistry.Register(new AnonSkinRegistry());
        CharacterSkinRegistry.Register(new RanaSkinRegistry());

        // 8.3 挂三个 UI 面板
        var skinReg = ModNodeAttachmentRegistry.For(ModId);

        skinReg.RegisterReadyChild<NCharacterSelectScreen, SkinPreviewPanel>(
            "skin_preview",
            static _ => new SkinPreviewPanel(),
            static (parent, node) => { },
            new NodeAttachmentOptions
            {
                Name = "SkinPreview", DuplicatePolicy = NodeAttachmentDuplicatePolicy.ReuseExistingByName, Order = 20
            });

        skinReg.RegisterReadyChild<NCharacterSelectScreen, DeckPresetPanel>(
            "deck_preset",
            static _ => new DeckPresetPanel(),
            static (parent, node) => { },
            new NodeAttachmentOptions
            {
                Name = "DeckPreset", DuplicatePolicy = NodeAttachmentDuplicatePolicy.ReuseExistingByName, Order = 21
            });

        skinReg.RegisterReadyChild<NCharacterSelectScreen, RelicSwitchPanel>(
            "relic_switch",
            static _ => new RelicSwitchPanel(),
            static (parent, node) => { },
            new NodeAttachmentOptions
            {
                Name = "RelicSwitch", DuplicatePolicy = NodeAttachmentDuplicatePolicy.ReuseExistingByName, Order = 22
            });

        // 8.4 生命周期：大厅贡献合并后刷新
        RitsuLibFramework.SubscribeLifecycle<RunSavedDataLobbyStagingEvent>(evt =>
        {
            if (evt.Reason == RunSavedDataLobbyStagingReason.ContributionMerged
                || evt.Reason == RunSavedDataLobbyStagingReason.PlayerJoined)
            {
                // ★ 不 forceReset：只更新远端缓存，不动本机已有选择
                SkinResolver.SyncFromLobby(evt.Lobby);
                SkinSystemEvents.RaiseCurrentCharacterChanged(SkinUiContext.CurrentCharacterType);
            }
        });

        // 9. 网络消息处理器 + 房主权威配置同步
        if (RunManager.Instance != null)
            RunManager.Instance.RunStarted += runState =>
            {
                var netService = RunManager.Instance?.NetService;
                if (netService != null)
                    RelicSwapCoordinator.Init(netService);
                // ★ 直接传 netService.NetId，避免依赖先前 SetLocalNetId 是否成功
                if (netService != null && runState != null)
                    SkinResolver.SyncFromRun(runState, netService.NetId);

                GameplayConfigSync.OnNetServiceReady(netService);

                if (netService != null)
                {
                    netService.RegisterMessageHandler(
                        new MessageHandlerDelegate<ReloadCountSyncMessage>((msg, senderId) =>
                            FlybackManager.OnReloadCountReceived(msg.ReloadCount)));

                    netService.RegisterMessageHandler(new MessageHandlerDelegate<NameChangeMessage>((msg, senderId) =>
                    {
                        var runState = NameChangeCmd.GetCurrentRunState();
                        if (runState != null)
                            PlayerNameData.PlayerNameSlot.Modify(runState, msg.TargetNetId,
                                data => { data.CustomName = msg.NewName; });
                        NameChangeCmd.RefreshAllPlayerNameUI();
                    }));

                    netService.RegisterMessageHandler(new MessageHandlerDelegate<ChordSyncMessage>((msg, senderId) =>
                    {
                        if (msg.PlayerNetId == netService.NetId) return;

                        var state = RunManager.Instance.DebugOnlyGetState();
                        if (state == null) return;
                        var player = state.Players.FirstOrDefault(p => p.NetId == msg.PlayerNetId);
                        if (player == null) return;
                        var chords = player.GetChords();
                        if (chords == null) return;

                        chords.RestoreChordData(msg.ChordsData, msg.BonusChordsData, "");
                        chords.SetLearnedChordsFromString(msg.LearnedChordsData);
                    }));

                    netService.RegisterMessageHandler(new MessageHandlerDelegate<SkinSyncMessage>((msg, senderId) =>
                    {
                        SkinSyncService.OnMessageReceived(msg, senderId);
                    }));

                    netService.RegisterMessageHandler(
                        new MessageHandlerDelegate<EggsGrantMessage>(async (msg, senderId) =>
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
        else
            Logger.Warn("RunManager.Instance is null, RunStarted event not subscribed.");

        // 图鉴入口：只保留两个聚合池（彩蛋 / 非彩蛋杂项），stable id 带 mod 前缀防撞车。
        ModContentRegistry.For(ModId)
            .RegisterCardLibraryCompendiumSharedPoolFilter<CuteSakikoCompendiumEggPool>(
                "cute_sakiko_compendium_egg_pool",
                "res://CuteSakikoMod/images/others/others/mod_card_pool_icon.png"
            );
        ModContentRegistry.For(ModId)
            .RegisterCardLibraryCompendiumSharedPoolFilter<CuteSakikoCompendiumJunkPool>(
                "cute_sakiko_compendium_junk_pool",
                "res://CuteSakikoMod/images/others/others/mod_token_card_pool_icon.png"
            );

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

        RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>(evt =>
        {
            if (!ModConfig.EggsCard) return;

            var netService = RunManager.Instance.NetService;
            var isHostOrSingle = netService?.Type is NetGameType.Singleplayer or NetGameType.Host;
            if (!isHostOrSingle) return;

            foreach (var player in evt.RunState.Players)
                if (!player.Relics.Any(r => r.Id == ModelDb.Relic<Eggs>().Id))
                    Logger.Warn($"[ConfigSync] 战斗开始时 player {player.NetId} 没有 Eggs（ActEnteredEvent 发放失败？）");
        });

        RitsuLibFramework.SubscribeLifecycle<RoomExitedEvent>(_ => { AudioManager.StopMusic(); });

        if (RunManager.Instance != null)
            RunManager.Instance.RunStarted += _ =>
            {
                if (CombatManager.Instance != null)
                    CombatManager.Instance.CombatEnded += _ => AudioManager.StopMusic();
            };

        _ = ChordNoteSystem.MaxStoredChords;
    }

    private static string GetSnakeCaseName(Type type)
    {
        var name = type.Name;
        var snake = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToLower();
        return snake;
    }
}
