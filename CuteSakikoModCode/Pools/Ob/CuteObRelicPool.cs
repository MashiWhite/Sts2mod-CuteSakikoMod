using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using CuteSakikoMod.CuteSakikoModCode.Others;
using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Pools.Ob;

public class CuteObRelicPool : TypeListRelicPoolModel
{
    public override Color LabOutlineColor => CuteOb.Color;
    public override string EnergyColorName => CuteOb.CharacterId;

    public override string BigEnergyIconPath => "charui/saki/saki_big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/saki/saki_text_energy.png".ImagePath();
}