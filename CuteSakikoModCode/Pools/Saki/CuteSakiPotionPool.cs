using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using Godot;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Pools.Saki;

public class CuteSakiPotionPool : TypeListPotionPoolModel
{
    public override Color LabOutlineColor => CuteSaki.Color;

    public override string EnergyColorName => CuteSaki.CharacterId;


    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}