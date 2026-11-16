using CuteSakikoMod.CuteSakikoModCode.Pools.Anon;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Character.Mygo;

[RegisterCharacter]
public class CuteAnon : CuteSakikoCharacter<CuteAnonCardPool, CuteAnonRelicPool, CuteAnonPotionPool>
{
    public const string CharacterId = "CUTEANON";
    public static readonly Color Color = new("#ff8899");

    protected override string? CharacterSelectBgPath =>
        "res://CuteSakikoMod/scenes/skin/mygo/anon/anon_bg.tscn";

    protected override string? CharacterSelectIconPath =>
        "res://CuteSakikoMod/images/charui/mygo/anon/char_select_anon.png";

    protected override string? CharacterSelectLockedIconPath =>
        "res://CuteSakikoMod/images/charui/mygo/anon/char_select_anon_locked.png";

    protected override string? MapMarkerPath =>
        "res://CuteSakikoMod/images/charui/mygo/anon/map_marker_anon.png";

    public override Color EnergyLabelOutlineColor => new(0f, 0.2f, 0.4f);

    public override Color NameColor => Color;
#if STS2_AT_LEAST_0_113_0
    // 0.113 Dread 模式：名字颜色 / 背景亮度与对比度（默认 0.7 / 1.1 / 金色）
    public override Godot.Color DreadNameColor => new(Color.R, Color.G, Color.B, 0.85f);
    public override float DreadBgBrightness => 0.74f;
    public override float DreadBgContrast => 1.08f;
#endif

    public override CharacterGender Gender => CharacterGender.Feminine;

    public override Color DialogueColor => new("#ff8899");
    public override Color MapDrawingColor => new("#ff8899");
    public override Color RemoteTargetingLineColor => new("#ff8899");
    public override Color RemoteTargetingLineOutline => new("#ff8899");

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