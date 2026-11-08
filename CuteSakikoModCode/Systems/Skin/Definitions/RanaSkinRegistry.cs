using CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Basic;
using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Core;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Definitions;

public sealed class RanaSkinRegistry : ICharacterSkinRegistry
{
    // ─────────────────────────────────────────────────────────
    // 共享预设（唯一来源）：该角色所有皮肤默认共用
    // ─────────────────────────────────────────────────────────
    private static readonly CharacterPresetSet SharedPresets = new(
        [
            new DeckPreset("rana_default", "CUTE_SAKIKO_MOD_RANA_DECK_DEFAULT",
            [
                (typeof(RanaStrike), 4),
                (typeof(RanaDefend), 4),
                (typeof(EatParfait), 1),
                (typeof(WantLive), 1)
            ])
        ],
        [
            new RelicPreset("rana_starter", "CUTE_SAKIKO_MOD_RANA_RELIC_DEFAULT", 70, 99, [typeof(MatchaParfait)])
        ]);

    public Type CharacterType => typeof(CuteRana);

    // ─────────────────────────────────────────────────────────
    // 皮肤：只写差异化字段（HP / Gold / Assets）
    // ─────────────────────────────────────────────────────────
    public IReadOnlyList<CharacterSkinDefinition> AllSkins { get; } =
    [
        CharacterSkinDefinition.FromPresets(
            "rana",
            "CUTE_SAKIKO_MOD_RANA_SKIN_DEFAULT",
            new SkinAssets(
                "res://CuteSakikoMod/scenes/char/rana/rana.tscn",
                "res://CuteSakikoMod/scenes/char/anon/anon_energy_counter.tscn",
                "res://CuteSakikoMod/scenes/char/rana/rana_merchant.tscn",
                "res://CuteSakikoMod/scenes/char/rana/rana_rest_site.tscn",
                "res://CuteSakikoMod/images/charui/rana/character_icon_rana.png",
                "res://CuteSakikoMod/images/charui/rana/character_icon_rana_outline.png",
                "res://CuteSakikoMod/scenes/char/rana/rana_icon.tscn",
                "res://CuteSakikoMod/scenes/ui/card_trail_anon.tscn",
                "res://CuteSakikoMod/images/charui/rana/multiplayer_hand_point.png",
                "res://CuteSakikoMod/images/charui/rana/multiplayer_hand_rock.png",
                "res://CuteSakikoMod/images/charui/rana/multiplayer_hand_paper.png",
                "res://CuteSakikoMod/images/charui/rana/multiplayer_hand_scissors.png"),
            SharedPresets),

        CharacterSkinDefinition.FromPresets(
            "tenday_rana",
            "CUTE_SAKIKO_MOD_RANA_SKIN_TENDAY",
            new SkinAssets(
                "res://CuteSakikoMod/scenes/skin/rana/tenday/tendayrana.tscn",
                "res://CuteSakikoMod/scenes/char/anon/anon_energy_counter.tscn",
                "res://CuteSakikoMod/scenes/skin/rana/tenday/tendayrana_merchant.tscn",
                "res://CuteSakikoMod/scenes/skin/rana/tenday/tendayrana_rest_site.tscn",
                "res://CuteSakikoMod/images/skin/rana/tenday/character_icon_rana.png",
                "res://CuteSakikoMod/images/skin/rana/tenday/character_icon_rana_outline.png",
                "res://CuteSakikoMod/scenes/skin/rana/tenday/tendayrana_icon.tscn",
                "res://CuteSakikoMod/scenes/ui/card_trail_anon.tscn",
                "res://CuteSakikoMod/images/skin/rana/tenday/multiplayer_hand_point.png",
                "res://CuteSakikoMod/images/skin/rana/tenday/multiplayer_hand_rock.png",
                "res://CuteSakikoMod/images/skin/rana/tenday/multiplayer_hand_paper.png",
                "res://CuteSakikoMod/images/skin/rana/tenday/multiplayer_hand_scissors.png"),
            SharedPresets)
    ];
}