using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Basic;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Planting;
using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using CuteSakikoMod.CuteSakikoModCode.Relics.Mutsumi.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Core;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Definitions;

public sealed class MutsumiSkinRegistry : ICharacterSkinRegistry
{
    private static readonly CharacterPresetSet SharedPresets = new(
        [
            new DeckPreset("mutsumi_default", "CUTE_SAKIKO_MOD_MUTSUMI_DECK_DEFAULT",
            [
                (typeof(MutsumiStrike), 4),
                (typeof(MutsumiDefend), 4),
                (typeof(PlantCucumber), 1)
            ]),
            new DeckPreset("mutsumi_mortis", "CUTE_SAKIKO_MOD_MUTSUMI_DECK_MORTIS",
            [
                (typeof(MutsumiStrike), 4),
                (typeof(MutsumiDefend), 4),
                (typeof(Aggressive), 1)
            ])
        ],
        [
            // 缄默 / 多言 是两个独立的遗物预设，玩家可自由搭配卡池流派
            new RelicPreset("mutsumi_silence", "CUTE_SAKIKO_MOD_MUTSUMI_RELIC_SILENCE", 75, 99, [typeof(SilenceRelic)]),
            new RelicPreset("mutsumi_talkative", "CUTE_SAKIKO_MOD_MUTSUMI_RELIC_TALKATIVE", 75, 99, [typeof(TalkativeRelic)])
        ]);

    public Type CharacterType => typeof(CuteMutsumi);

    public IReadOnlyList<CharacterSkinDefinition> AllSkins { get; } =
    [
        CharacterSkinDefinition.FromPresets(
            "tsukinomori_high_winter",
            "CUTE_SAKIKO_MOD_MUTSUMI_SKIN_DEFAULT",
            new SkinAssets(
                "res://CuteSakikoMod/scenes/skin/mujica/mutsumi/tsukinomori_high_winter/mutsumi.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/mujica_energy_counter.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/mutsumi/tsukinomori_high_winter/mutsumi_merchant.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/mutsumi/tsukinomori_high_winter/mutsumi_rest_site.tscn",
                "res://CuteSakikoMod/images/skin/mujica/mutsumi/tsukinomori_high_winter/character_icon_mutsumi.png",
                "res://CuteSakikoMod/images/skin/mujica/mutsumi/tsukinomori_high_winter/character_icon_mutsumi_outline.png",
                "res://CuteSakikoMod/scenes/skin/mujica/mutsumi/tsukinomori_high_winter/mutsumi_icon.tscn",
                "res://CuteSakikoMod/scenes/skin/mujica/mutsumi/card_trail_mutsumi.tscn",
                "res://CuteSakikoMod/images/skin/mujica/mutsumi/tsukinomori_high_winter/multiplayer_hand_point.png",
                "res://CuteSakikoMod/images/skin/mujica/mutsumi/tsukinomori_high_winter/multiplayer_hand_rock.png",
                "res://CuteSakikoMod/images/skin/mujica/mutsumi/tsukinomori_high_winter/multiplayer_hand_paper.png",
                "res://CuteSakikoMod/images/skin/mujica/mutsumi/tsukinomori_high_winter/multiplayer_hand_scissors.png"),
            SharedPresets)
    ];
}
