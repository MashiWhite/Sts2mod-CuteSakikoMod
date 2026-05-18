using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Character;

[RegisterCharacter]
public class CuteSaki : ModCharacterTemplate<CuteSakiCardPool, CuteSakiRelicPool, CuteSakiPotionPool>
{
    public const string CharacterId = "CUTESAKI";
    public const string CharacterEggId = "SAKIEGGS";
    public static readonly Color Color = new("#7799cc");
    public override Color EnergyLabelOutlineColor => new(1f, 0f, 0f);

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 65;
    public override int StartingGold => 120;

    public override CharacterAssetProfile AssetProfile => CharacterAssetProfiles.Merge(
        CharacterAssetProfiles.Ironclad(),
        new CharacterAssetProfile(
            new CharacterSceneAssetSet(
                // 人物模型tscn路径。
                "res://CuteSakikoMod/scenes/char/saki/sakiko.tscn",
                // 能量表盘tscn路径。
                "res://CuteSakikoMod/scenes/char/saki/saki_energy_counter.tscn",
                // 商店人物场景。
                "res://CuteSakikoMod/scenes/char/saki/saki_merchant.tscn",
                // 篝火休息场景。
                "res://CuteSakikoMod/scenes/char/saki/saki_rest_site.tscn"
            ),
            new CharacterUiAssetSet(
                // 人物头像路径。
                "res://CuteSakikoMod/images/charui/saki/character_icon_saki.png",
                // 人物头像2号。
                IconPath: "res://CuteSakikoMod/scenes/char/saki/saki_icon.tscn",
                // 人物选择背景。
                CharacterSelectBgPath: "res://CuteSakikoMod/scenes/char/saki/saki_bg.tscn",
                // 人物选择图标。
                CharacterSelectIconPath: "res://CuteSakikoMod/images/charui/saki/char_select_saki.png",
                // 人物选择图标-锁定状态。
                CharacterSelectLockedIconPath: "res://CuteSakikoMod/images/charui/saki/char_select_saki_locked.png",
                // 人物选择过渡动画。
                // CharacterSelectTransitionPath: "res://materials/transitions/ironclad_transition_mat.tres",
                // 地图上的角色标记图标、表情轮盘上的角色头像
                MapMarkerPath: "res://CuteSakikoMod/images/charui/saki/map_marker_saki.png"
            ),
            new CharacterVfxAssetSet(
                // 卡牌拖尾场景。
                "res://CuteSakikoMod/scenes/ui/card_trail_sakiko.tscn"
            ),
            Audio: new CharacterAudioAssetSet(
                // 攻击音效
                // AttackSfx: null,
                // 施法音效
                // CastSfx: null,
                // 死亡音效
                // DeathSfx: null,
                // 角色选择音效
                // CharacterSelectSfx: null,
                // 过渡音效
                // CharacterTransitionSfx: "event:/sfx/ui/wipe_ironclad"
            ),
            Multiplayer: new CharacterMultiplayerAssetSet(
                // 多人模式-手指。
                "res://CuteSakikoMod/images/others/saki/multiplayer_hand_point.png",
                // 多人模式剪刀石头布-石头。
                "res://CuteSakikoMod/images/others/saki/multiplayer_hand_rock.png",
                // 多人模式剪刀石头布-布。
                "res://CuteSakikoMod/images/others/saki/multiplayer_hand_paper.png",
                // 多人模式剪刀石头布-剪刀。
                "res://CuteSakikoMod/images/others/saki/multiplayer_hand_scissors.png"
            )));


    public override Color DialogueColor => new("#7799cc");
    public override Color MapDrawingColor => new("#7799cc");
    public override Color RemoteTargetingLineColor => new("#7799cc");
    public override Color RemoteTargetingLineOutline => new("#7799cc");

    //不需要解锁时间线
    public override bool RequiresEpochAndTimeline => false;

    //对齐动画延迟
    public override float AttackAnimDelay => 0f;
    public override float CastAnimDelay => 0f;

    //自动转换角色tscn节点
    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.Scenes!.VisualsPath!);
    }

    //攻击建筑师特效
    public override List<string> GetArchitectAttackVfx()
    {
        return
        [
            "vfx/vfx_attack_blunt",
            "vfx/vfx_heavy_blunt",
            "vfx/vfx_attack_slash",
            "vfx/vfx_bloody_impact",
            "vfx/vfx_rock_shatter"
        ];
    }
}