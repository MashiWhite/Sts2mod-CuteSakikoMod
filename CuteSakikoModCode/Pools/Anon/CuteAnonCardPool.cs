using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using Godot;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Utils;

namespace CuteSakikoMod.CuteSakikoModCode.Pools.Anon;

public class CuteAnonCardPool : TypeListCardPoolModel
{
    private static readonly Material? _poolFrameMaterial = MaterialUtils.CreateRgbShaderMaterial(0.635f, 0.772f, 0.82f);
    public override string Title => CuteAnon.CharacterId; //This is not a display name.
    public override string EnergyColorName => CuteAnon.CharacterId;
    public override string? BigEnergyIconPath => "charui/anon/anon_big_energy.png".ImagePath();
    public override string? TextEnergyIconPath => "charui/anon/anon_text_energy.png".ImagePath();
    public override Color EnergyOutlineColor => new(0f, 0.2f, 0.4f);
    public override Material? PoolFrameMaterial => _poolFrameMaterial;

    //Alternatively, leave these values at 1 and provide a custom frame image.
    /*public override Texture2D CustomFrame(CustomCardModel card)
    {
        //This will attempt to load CuteSakikoMod/images/cards/frame.png
        return PreloadManager.Cache.GetTexture2D("cards/frame.png".ImagePath());
    }*/

    //Color of small card icons
    public override Color DeckEntryCardColor => new("#56627d");
    public override bool IsColorless => false;
}