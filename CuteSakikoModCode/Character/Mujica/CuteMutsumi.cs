using CuteSakikoMod.CuteSakikoModCode.Pools.Mutsumi;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Character.Mujica;

[RegisterCharacter]
public class CuteMutsumi : CuteSakikoCharacter<CuteMutsumiCardPool, CuteMutsumiRelicPool, CuteMutsumiPotionPool>
{
    public const string CharacterId = "CUTEMUTSUMI";
    public static readonly Color Color = new("#779977");

    protected override string? CharacterSelectBgPath =>
        "res://CuteSakikoMod/scenes/skin/mujica/mutsumi/mutsumi_bg.tscn";

    protected override string? CharacterSelectIconPath =>
        "res://CuteSakikoMod/images/charui/mujica/mutsumi/char_select_mutsumi.png";

    protected override string? CharacterSelectLockedIconPath =>
        "res://CuteSakikoMod/images/charui/mujica/mutsumi/char_select_mutsumi_locked.png";

    protected override string? MapMarkerPath =>
        "res://CuteSakikoMod/images/charui/mujica/mutsumi/map_marker_mutsumi.png";

    public override Color EnergyLabelOutlineColor => new(0f, 0.2f, 0.2f);

    public override Color NameColor => Color;
#if STS2_AT_LEAST_0_113_0
    // 0.113 Dread 模式：名字颜色 / 背景亮度与对比度
    public override Godot.Color DreadNameColor => new(Color.R, Color.G, Color.B, 0.85f);
    public override float DreadBgBrightness => 0.70f;
    public override float DreadBgContrast => 1.12f;
#endif

    public override CharacterGender Gender => CharacterGender.Feminine;

    public override Color DialogueColor => new("#4a6b4a");
    public override Color MapDrawingColor => new("#779977");
    public override Color RemoteTargetingLineColor => new("#779977");
    public override Color RemoteTargetingLineOutline => new("#779977");

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
