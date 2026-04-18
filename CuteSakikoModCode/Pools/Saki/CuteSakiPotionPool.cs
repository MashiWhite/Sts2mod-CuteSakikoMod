using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Pools.Saki;

public class CuteSakiPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => Character.CuteSaki.Color;


    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}