using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using CuteSakikoMod.CuteSakikoModCode.Others;
using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Pools.Mutsumi;

public class CuteMutsumiPotionPool : TypeListPotionPoolModel
{
    public override Color LabOutlineColor => CuteMutsumi.Color;
    public override string EnergyColorName => CuteMutsumi.CharacterId;
    public override string BigEnergyIconPath => "charui/mujica/mujica_big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/mujica/mujica_text_energy.png".ImagePath();
}
