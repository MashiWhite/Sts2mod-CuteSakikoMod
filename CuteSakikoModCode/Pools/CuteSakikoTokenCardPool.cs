using CuteSakikoMod.CuteSakikoModCode.Others;
using Godot;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace CuteSakikoMod.CuteSakikoModCode.Pools;

[RegisterSharedCardPool]
public class CuteSakikoTokenCardPool : TypeListCardPoolModel
{
    private static readonly Material?
        _poolFrameMaterial = MaterialUtils.CreateReplaceHueShaderMaterial(0.5373f, 0.7725f, 0.9294f, 0.8f);

    public override string Title => "CuteSakikoTokenCard";
    public override string EnergyColorName => "CuteSakikoModTokenBlue";

    public override string BigEnergyIconPath => "others/others/mod_card_pool_big_energy_icon.png".ImagePath();
    public override string TextEnergyIconPath => "others/others/mod_card_pool_energy_icon.png".ImagePath();
    public override Color EnergyOutlineColor => new("#1B75B1");
    public override Material? PoolFrameMaterial => _poolFrameMaterial;

    public override Color DeckEntryCardColor => new("#C4E2F6");

    public override bool IsColorless => true;
}