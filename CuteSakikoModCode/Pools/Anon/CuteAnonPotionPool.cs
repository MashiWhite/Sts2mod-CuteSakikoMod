using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Pools.Anon;

public class CuteAnonPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => Character.CuteAnon.Color;


    public override string BigEnergyIconPath => "charui/anon_big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/anon_text_energy.png".ImagePath();
}