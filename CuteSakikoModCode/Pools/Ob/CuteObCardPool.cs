using CuteSakikoMod.CuteSakikoModCode.Cards.Saki;
using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using CuteSakikoMod.CuteSakikoModCode.Others;
using Godot;
using STS2RitsuLib.Scaffolding.Characters;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace CuteSakikoMod.CuteSakikoModCode.Pools.Ob;

public class CuteObCardPool : TypeListCardPoolModel, IModColorfulPhilosophersCardPool
{
    private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateReplaceHueShaderMaterial(0.4666667f, 0.6f, 0.8f);
    public override string Title => CuteOb.CharacterId; //This is not a display name.
    public override string EnergyColorName => CuteOb.CharacterId;
    public override string BigEnergyIconPath => "charui/saki/saki_big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/saki/saki_text_energy.png".ImagePath();
    public override Color EnergyOutlineColor => new("#420000");
    public override Material? PoolFrameMaterial => _poolFrameMaterial;

    public override Color DeckEntryCardColor => new("#7799CC");

    public override bool IsColorless => false;
    
    private static readonly Type[] _allSakiCardTypes = 
        typeof(CuteSakikoModCard).Assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(CuteSakikoModCard).IsAssignableFrom(t))
            .ToArray();

    protected override IEnumerable<Type> CardTypes => _allSakiCardTypes;
}