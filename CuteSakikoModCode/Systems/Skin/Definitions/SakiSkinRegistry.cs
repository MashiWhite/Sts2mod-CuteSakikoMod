using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Basic;
using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Core;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Definitions;

public sealed class SakiSkinRegistry : ICharacterSkinRegistry
{
    // ─────────────────────────────────────────────────────────
    //                      牌组/遗物血量
    // ─────────────────────────────────────────────────────────
    private static readonly CharacterPresetSet SharedPresets = new(
        [
            new DeckPreset("saki_default", "CUTE_SAKIKO_MOD_SAKI_DECK_DEFAULT",
            [
                (typeof(PianoStrike), 4),
                (typeof(DefendSaki), 4),
                (typeof(GoWork), 1)
            ]),
            new DeckPreset("ob_default", "CUTE_SAKIKO_MOD_SAKI_DECK_OB",
            [
                (typeof(PianoStrike), 4),
                (typeof(DefendSaki), 4),
                (typeof(TemporaryForgetfulness), 1)
            ])
        ],
        [
            new RelicPreset("saki_starter", "CUTE_SAKIKO_MOD_SAKI_RELIC_DEFAULT", 80, 88, [typeof(KabutoNote)]),
            new RelicPreset("ob_starter", "CUTE_SAKIKO_MOD_SAKI_RELIC_OB", 70, 111, [typeof(ObMask)])
        ]);

    public Type CharacterType => typeof(CuteSaki);

    // ─────────────────────────────────────────────────────────
    //                         皮肤
    // ─────────────────────────────────────────────────────────
    public IReadOnlyList<CharacterSkinDefinition> AllSkins { get; } =
    [
        CharacterSkinDefinition.FromPresets(
            "haneoka_high_1st_winter",
            "CUTE_SAKIKO_MOD_SAKI_SKIN_DEFAULT",
            new SkinAssets(
                "res://CuteSakikoMod/scenes/skin/mujica/saki/haneoka_high_1st_winter/sakiko.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/mujica_energy_counter.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/saki/haneoka_high_1st_winter/saki_merchant.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/saki/haneoka_high_1st_winter/saki_rest_site.tscn",
                "res://CuteSakikoMod/images/skin/mujica/saki/haneoka_high_1st_winter/character_icon_saki.png",
                "res://CuteSakikoMod/images/skin/mujica/saki/haneoka_high_1st_winter/character_icon_saki_outline.png",
                "res://CuteSakikoMod/scenes/skin/mujica/saki/haneoka_high_1st_winter/saki_icon.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/saki/card_trail_sakiko.tscn",
                "res://CuteSakikoMod/images/skin/mujica/saki/haneoka_high_1st_winter/multiplayer_hand_point.png",
                "res://CuteSakikoMod/images/skin/mujica/saki/haneoka_high_1st_winter/multiplayer_hand_rock.png",
                "res://CuteSakikoMod/images/skin/mujica/saki/haneoka_high_1st_winter/multiplayer_hand_paper.png",
                "res://CuteSakikoMod/images/skin/mujica/saki/haneoka_high_1st_winter/multiplayer_hand_scissors.png"),
            SharedPresets),

        CharacterSkinDefinition.FromPresets(
            "ob",
            "CUTE_SAKIKO_MOD_SAKI_SKIN_OB",
            new SkinAssets(
                "res://CuteSakikoMod/scenes/skin/mujica/saki/ob/ob.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/mujica_energy_counter.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/saki/ob/ob_merchant.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/saki/ob/ob_rest_site.tscn",
                "res://CuteSakikoMod/images/skin/mujica/saki/ob/character_icon_ob.png",
                "res://CuteSakikoMod/images/skin/mujica/saki/ob/character_icon_ob_outline.png",
                "res://CuteSakikoMod/scenes/skin/mujica/saki/ob/ob_icon.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/saki/card_trail_sakiko.tscn",
                "res://CuteSakikoMod/images/skin/mujica/saki/ob/multiplayer_hand_point.png",
                "res://CuteSakikoMod/images/skin/mujica/saki/ob/multiplayer_hand_rock.png",
                "res://CuteSakikoMod/images/skin/mujica/saki/ob/multiplayer_hand_paper.png",
                "res://CuteSakikoMod/images/skin/mujica/saki/ob/multiplayer_hand_scissors.png"),
            SharedPresets),
        
        CharacterSkinDefinition.FromPresets(
        "tenday_saki",
        "CUTE_SAKIKO_MOD_SAKI_SKIN_TENDAY",
        new SkinAssets(
            "res://CuteSakikoMod/scenes/skin/mujica/saki/tenday_saki/tendaysaki.tscn",
            "res://CuteSakikoMod/scenes/skin/mujica/mujica_energy_counter.tscn",
            "res://CuteSakikoMod/scenes/skin/mujica/saki/tenday_saki/tendaysaki_merchant.tscn",
            "res://CuteSakikoMod/scenes/skin/mujica/saki/tenday_saki/tendaysaki_rest_site.tscn",
            "res://CuteSakikoMod/images/skin/mujica/saki/tenday_saki/character_icon_saki.png",
            "res://CuteSakikoMod/images/skin/mujica/saki/tenday_saki/character_icon_saki_outline.png",
            "res://CuteSakikoMod/scenes/skin/mujica/saki/tenday_saki/tendaysaki_icon.tscn",
            "res://CuteSakikoMod/scenes/skin/mujica/saki/card_trail_sakiko.tscn",
            "res://CuteSakikoMod/images/skin/mujica/saki/tenday_saki/multiplayer_hand_point.png",
            "res://CuteSakikoMod/images/skin/mujica/saki/tenday_saki/multiplayer_hand_rock.png",
            "res://CuteSakikoMod/images/skin/mujica/saki/tenday_saki/multiplayer_hand_paper.png",
            "res://CuteSakikoMod/images/skin/mujica/saki/tenday_saki/multiplayer_hand_scissors.png"),
        SharedPresets),

        CharacterSkinDefinition.FromPresets(
            "godsblood",
            "CUTE_SAKIKO_MOD_SAKI_SKIN_GODSBLOOD",
            new SkinAssets(
                "res://CuteSakikoMod/scenes/skin/mujica/saki/godsblood/saki.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/mujica_energy_counter.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/saki/godsblood/saki_merchant.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/saki/godsblood/saki_rest_site.tscn",
                "res://CuteSakikoMod/images/skin/mujica/saki/godsblood/character_icon_saki.png",
                "res://CuteSakikoMod/images/skin/mujica/saki/godsblood/character_icon_saki_outline.png",
                "res://CuteSakikoMod/scenes/skin/mujica/saki/godsblood/saki_icon.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/saki/card_trail_sakiko.tscn",
                "res://CuteSakikoMod/images/skin/mujica/saki/godsblood/multiplayer_hand_point.png",
                "res://CuteSakikoMod/images/skin/mujica/saki/godsblood/multiplayer_hand_rock.png",
                "res://CuteSakikoMod/images/skin/mujica/saki/godsblood/multiplayer_hand_paper.png",
                "res://CuteSakikoMod/images/skin/mujica/saki/godsblood/multiplayer_hand_scissors.png"),
            SharedPresets),
        
        CharacterSkinDefinition.FromPresets("haneoka_high_2nd_winter",
            "CUTE_SAKIKO_MOD_SAKI_SKIN_HANEOKA_2ND_WINTER",
            new SkinAssets(
                "res://CuteSakikoMod/scenes/skin/mujica/saki/haneoka_high_2nd_winter/saki.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/mujica_energy_counter.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/saki/haneoka_high_2nd_winter/saki_merchant.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/saki/haneoka_high_2nd_winter/saki_rest_site.tscn",
                "res://CuteSakikoMod/images/skin/mujica/saki/haneoka_high_2nd_winter/character_icon_saki.png",
                "res://CuteSakikoMod/images/skin/mujica/saki/haneoka_high_2nd_winter/character_icon_saki_outline.png",
                "res://CuteSakikoMod/scenes/skin/mujica/saki/haneoka_high_2nd_winter/saki_icon.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/saki/card_trail_sakiko.tscn",
                "res://CuteSakikoMod/images/skin/mujica/saki/haneoka_high_2nd_winter/multiplayer_hand_point.png",
                "res://CuteSakikoMod/images/skin/mujica/saki/haneoka_high_2nd_winter/multiplayer_hand_rock.png",
                "res://CuteSakikoMod/images/skin/mujica/saki/haneoka_high_2nd_winter/multiplayer_hand_paper.png",
                "res://CuteSakikoMod/images/skin/mujica/saki/haneoka_high_2nd_winter/multiplayer_hand_scissors.png"),
            SharedPresets)
    ];
}