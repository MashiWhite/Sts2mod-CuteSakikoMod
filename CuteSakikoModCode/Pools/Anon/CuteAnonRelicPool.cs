using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Others;
using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Pools.Anon;

public class CuteAnonRelicPool : TypeListRelicPoolModel
{
    public override Color LabOutlineColor => CuteAnon.Color;
    public override string EnergyColorName => CuteAnon.CharacterId;
    public override string? BigEnergyIconPath => "charui/mygo/mygo_big_energy.png".ImagePath();
    public override string? TextEnergyIconPath => "charui/mygo/mygo_text_energy.png".ImagePath();
}