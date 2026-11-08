using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Core;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Definitions;

public sealed class AnonSkinRegistry : ICharacterSkinRegistry
{
    // ─────────────────────────────────────────────────────────
    // 共享预设（唯一来源）：该角色所有皮肤默认共用
    // ─────────────────────────────────────────────────────────
    private static readonly CharacterPresetSet SharedPresets = new(
        [
            new DeckPreset("anon_default", "CUTE_SAKIKO_MOD_ANON_DECK_DEFAULT",
            [
                (typeof(AnonStrike), 4),
                (typeof(AnonDefend), 4),
                (typeof(TurnIntoNote), 1),
                (typeof(PlayChord), 1)
            ])
        ],
        [
            new RelicPreset("anon_starter", "CUTE_SAKIKO_MOD_ANON_RELIC_DEFAULT", 70, 99, [typeof(AnonGuitar)])
        ]);

    public Type CharacterType => typeof(CuteAnon);

    // ─────────────────────────────────────────────────────────
    // 皮肤：只写差异化字段（HP / Gold / Assets）
    // ─────────────────────────────────────────────────────────
    public IReadOnlyList<CharacterSkinDefinition> AllSkins { get; } =
    [
        CharacterSkinDefinition.FromPresets(
            "anon",
            "CUTE_SAKIKO_MOD_ANON_SKIN_DEFAULT",
            new SkinAssets(
                "res://CuteSakikoMod/scenes/char/anon/anon.tscn",
                "res://CuteSakikoMod/scenes/char/anon/anon_energy_counter.tscn",
                "res://CuteSakikoMod/scenes/char/anon/anon_merchant.tscn",
                "res://CuteSakikoMod/scenes/char/anon/anon_rest_site.tscn",
                "res://CuteSakikoMod/images/charui/anon/character_icon_anon.png",
                "res://CuteSakikoMod/images/charui/anon/character_icon_anon_outline.png",
                "res://CuteSakikoMod/scenes/char/anon/anon_icon.tscn",
                "res://CuteSakikoMod/scenes/ui/card_trail_anon.tscn",
                "res://CuteSakikoMod/images/charui/saki/multiplayer_hand_point.png",
                "res://CuteSakikoMod/images/charui/saki/multiplayer_hand_rock.png",
                "res://CuteSakikoMod/images/charui/saki/multiplayer_hand_paper.png",
                "res://CuteSakikoMod/images/charui/saki/multiplayer_hand_scissors.png"),
            SharedPresets),

        CharacterSkinDefinition.FromPresets(
            "tenday_anon",
            "CUTE_SAKIKO_MOD_ANON_SKIN_TENDAY",
            new SkinAssets(
                "res://CuteSakikoMod/scenes/skin/anon/tenday/tendayanon.tscn",
                "res://CuteSakikoMod/scenes/char/anon/anon_energy_counter.tscn",
                "res://CuteSakikoMod/scenes/skin/anon/tenday/tendayanon_merchant.tscn",
                "res://CuteSakikoMod/scenes/skin/anon/tenday/tendayanon_rest_site.tscn",
                "res://CuteSakikoMod/images/skin/anon/tenday/character_icon_anon.png",
                "res://CuteSakikoMod/images/skin/anon/tenday/character_icon_anon_outline.png",
                "res://CuteSakikoMod/scenes/skin/anon/tenday/tendayanon_icon.tscn",
                "res://CuteSakikoMod/scenes/ui/card_trail_anon.tscn",
                "res://CuteSakikoMod/images/skin/anon/tenday/multiplayer_hand_point.png",
                "res://CuteSakikoMod/images/skin/anon/tenday/multiplayer_hand_rock.png",
                "res://CuteSakikoMod/images/skin/anon/tenday/multiplayer_hand_paper.png",
                "res://CuteSakikoMod/images/skin/anon/tenday/multiplayer_hand_scissors.png"),
            SharedPresets),

        // 阿侬汪（2026-11-09 新增）：复用 SharedPresets（初始牌组/遗物/HP 金币与默认皮肤一致），
        // 只换美术资源。
        CharacterSkinDefinition.FromPresets(
            "anonwang",
            "CUTE_SAKIKO_MOD_ANON_SKIN_ANONWANG",
            new SkinAssets(
                "res://CuteSakikoMod/scenes/skin/anon/anonwang/anonwang.tscn",
                "res://CuteSakikoMod/scenes/char/anon/anon_energy_counter.tscn",
                "res://CuteSakikoMod/scenes/skin/anon/anonwang/anonwang_merchant.tscn",
                "res://CuteSakikoMod/scenes/skin/anon/anonwang/anonwang_rest_site.tscn",
                "res://CuteSakikoMod/images/skin/anon/anonwang/character_icon_anon.png",
                "res://CuteSakikoMod/images/skin/anon/anonwang/character_icon_anon_outline.png",
                "res://CuteSakikoMod/scenes/skin/anon/anonwang/anonwang_icon.tscn",
                "res://CuteSakikoMod/scenes/ui/card_trail_anon.tscn",
                "res://CuteSakikoMod/images/skin/anon/anonwang/multiplayer_hand_point.png",
                "res://CuteSakikoMod/images/skin/anon/anonwang/multiplayer_hand_rock.png",
                "res://CuteSakikoMod/images/skin/anon/anonwang/multiplayer_hand_paper.png",
                "res://CuteSakikoMod/images/skin/anon/anonwang/multiplayer_hand_scissors.png"),
            SharedPresets)
    ];
}