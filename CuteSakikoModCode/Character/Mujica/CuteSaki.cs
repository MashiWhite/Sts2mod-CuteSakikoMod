using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Character.Mujica;

[RegisterCharacter]
public class CuteSaki : CuteSakikoCharacter<CuteSakiCardPool, CuteSakiRelicPool, CuteSakiPotionPool>
{
    public const string CharacterId = "CUTESAKI";
    public static readonly Color Color = new("#7799cc");

    // ★ 皮肤系统入口
    protected override string? CharacterSelectBgPath =>
        "res://CuteSakikoMod/scenes/skin/mujica/saki/saki_bg.tscn";

    protected override string? CharacterSelectIconPath =>
        "res://CuteSakikoMod/images/charui/mujica/saki/char_select_saki.png";

    protected override string? CharacterSelectLockedIconPath =>
        "res://CuteSakikoMod/images/charui/mujica/saki/char_select_saki_locked.png";

    protected override string? MapMarkerPath =>
        "res://CuteSakikoMod/images/charui/mujica/saki/map_marker_saki.png";

    public override Color EnergyLabelOutlineColor => new(1f, 0f, 0f);
    public override Color NameColor => Color;
#if STS2_AT_LEAST_0_113_0
    // 0.113 Dread 模式：名字颜色 / 背景亮度与对比度（默认 0.7 / 1.1 / 金色）
    public override Godot.Color DreadNameColor => new(Color.R, Color.G, Color.B, 0.85f);
    public override float DreadBgBrightness => 0.66f;
    public override float DreadBgContrast => 1.15f;
#endif

    public override CharacterGender Gender => CharacterGender.Feminine;

    public override Color DialogueColor => Color;
    public override Color MapDrawingColor => Color;
    public override Color RemoteTargetingLineColor => Color;
    public override Color RemoteTargetingLineOutline => Color;

    public override bool RequiresEpochAndTimeline => false;
    public override float AttackAnimDelay => 0f;
    public override float CastAnimDelay => 0f;

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            AssetProfile.Scenes!.VisualsPath!);
    }

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