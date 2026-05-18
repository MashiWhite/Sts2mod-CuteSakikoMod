using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using Godot;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace CuteSakikoMod.CuteSakikoModCode.Pools.Saki;

public class CuteSakiCardPool : TypeListCardPoolModel, IModColorfulPhilosophersCardPool
{
    private static readonly Material?
        _poolFrameMaterial = MaterialUtils.CreateRgbShaderMaterial(0.502f, 0f, 0f);

    public override string Title => CuteSaki.CharacterId; //This is not a display name.
    public override string EnergyColorName => CuteSaki.CharacterId;
    public override string BigEnergyIconPath => "charui/saki/saki_big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/saki/saki_text_energy.png".ImagePath();
    public override Color EnergyOutlineColor => new("#420000");
    public override Material? PoolFrameMaterial => _poolFrameMaterial;

    public override Color DeckEntryCardColor => new("#800000");

    public override bool IsColorless => false;
}