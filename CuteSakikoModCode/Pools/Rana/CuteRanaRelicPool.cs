using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Others;
using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Pools.Rana;

public class CuteRanaRelicPool : TypeListRelicPoolModel
{
    public override Color LabOutlineColor => CuteAnon.Color;
    public override string EnergyColorName => CuteAnon.CharacterId;
    public override string? BigEnergyIconPath => "charui/anon/anon_big_energy.png".ImagePath();
    public override string? TextEnergyIconPath => "charui/anon/anon_text_energy.png".ImagePath();
}