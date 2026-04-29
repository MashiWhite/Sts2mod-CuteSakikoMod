using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Pools.Anon;

public class CuteAnonRelicPool : TypeListRelicPoolModel
{
    public override Color LabOutlineColor => CuteAnon.Color;
    public override string EnergyColorName => CuteAnon.CharacterId;
    public override string? BigEnergyIconPath => "charui/anon_big_energy.png".ImagePath();
    public override string? TextEnergyIconPath => "charui/anon_text_energy.png".ImagePath();
}