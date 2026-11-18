using CuteSakikoMod.CuteSakikoModCode.Others;
using Godot;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace CuteSakikoMod.CuteSakikoModCode.Pools.Mutsumi;

/// <summary>
///     睦（若叶睦）的卡池。睦的角色类尚未实现，这里先提供卡池，角色接入时把
///     <see cref="CharacterId" /> 指向 CuteMutsumi.CharacterId 即可。
/// </summary>
public class CuteMutsumiCardPool : TypeListCardPoolModel, IModColorfulPhilosophersCardPool
{
    public const string CharacterId = "CUTEMUTSUMI";

    // 睦的代表色 #779977
    private static readonly Material? _poolFrameMaterial =
        MaterialUtils.CreateReplaceHueShaderMaterial(0.467f, 0.6f, 0.467f);

    public override string Title => CharacterId; //This is not a display name.
    public override string EnergyColorName => CharacterId;
    public override string? BigEnergyIconPath => "charui/mujica/mujica_big_energy.png".ImagePath();
    public override string? TextEnergyIconPath => "charui/mujica/mujica_text_energy.png".ImagePath();
    public override Color EnergyOutlineColor => new(0f, 0.2f, 0.2f);
    public override Material? PoolFrameMaterial => _poolFrameMaterial;
    public override Color DeckEntryCardColor => new("#779977");
    public override bool IsColorless => false;
}
