using CuteSakikoMod.CuteSakikoModCode.Pools.Anon;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Character;

[RegisterCharacter]
public class CuteAnon : ModCharacterTemplate<CuteAnonCardPool, CuteAnonRelicPool, CuteAnonPotionPool>
{
    public const string CharacterId = "千早爱音";
    public static readonly Color Color = new("#ff8899");
    public override Color EnergyLabelOutlineColor => new(0f, 0.2f, 0.4f);

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 70;
    public override int StartingGold => 99;

    public override CharacterAssetProfile AssetProfile => CharacterAssetProfiles.Merge(
        CharacterAssetProfiles.Ironclad(),
        new CharacterAssetProfile(
            new CharacterSceneAssetSet(
                // 人物模型tscn路径。
                "res://CuteSakikoMod/scenes/char/anon/anon.tscn",
                // 能量表盘tscn路径。
                "res://CuteSakikoMod/scenes/char/anon/anon_energy_counter.tscn",
                // 商店人物场景。
                "res://CuteSakikoMod/scenes/char/anon/anon_merchant.tscn",
                // 篝火休息场景。
                "res://CuteSakikoMod/scenes/char/anon/anon_rest_site.tscn"
            ),
            new CharacterUiAssetSet(
                // 人物头像路径。
                "res://CuteSakikoMod/images/charui/anon/character_icon_anon.png",
                // 人物头像2号。
                IconPath: "res://CuteSakikoMod/scenes/char/anon/anon_icon.tscn",
                // 人物选择背景。
                CharacterSelectBgPath: "res://CuteSakikoMod/scenes/char/anon/anon_bg.tscn",
                // 人物选择图标。
                CharacterSelectIconPath: "res://CuteSakikoMod/images/charui/anon/char_select_anon.png",
                // 人物选择图标-锁定状态。
                CharacterSelectLockedIconPath: "res://CuteSakikoMod/images/charui/anon/char_select_anon_locked.png",
                // 人物选择过渡动画。
                // CharacterSelectTransitionPath: "res://materials/transitions/ironclad_transition_mat.tres",
                // 地图上的角色标记图标、表情轮盘上的角色头像
                MapMarkerPath: "res://CuteSakikoMod/images/charui/anon/map_marker_anon.png"
            ),
            new CharacterVfxAssetSet(
                // 卡牌拖尾场景。
                // TrailPath: "res://scenes/vfx/card_trail_ironclad.tscn"
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
                "res://CuteSakikoMod/images/others/anon/multiplayer_hand_point.png",
                // 多人模式剪刀石头布-石头。
                "res://CuteSakikoMod/images/others/anon/multiplayer_hand_rock.png",
                // 多人模式剪刀石头布-布。
                "res://CuteSakikoMod/images/others/anon/multiplayer_hand_paper.png",
                // 多人模式剪刀石头布-剪刀。
                "res://CuteSakikoMod/images/others/anon/multiplayer_hand_scissors.png"
            )));


    //一堆有关颜色
    public override Color DialogueColor => new("#ff8899");
    public override Color MapDrawingColor => new("#ff8899");
    public override Color RemoteTargetingLineColor => new("#ff8899");
    public override Color RemoteTargetingLineOutline => new("#ff8899");

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