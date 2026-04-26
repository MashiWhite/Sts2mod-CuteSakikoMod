using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Pools;

public class CuteSakikoEggCardPool : CustomCardPoolModel
{
    public override string Title => Character.CuteSaki.CharacterEggId; //This is not a display name.

    public override string BigEnergyIconPath => "charui/saki/saki_big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/saki/saki_text_energy.png".ImagePath();


    /* These HSV values will determine the color of your card back.
    They are applied as a shader onto an already colored image,
    so it may take some experimentation to find a color you like.
    Generally they should be values between 0 and 1. */
    public override float H => 2.1f; //Hue; changes the color.
    public override float S => 0.65f; //Saturation
    public override float V => 0.37f; //Brightness

    //Alternatively, leave these values at 1 and provide a custom frame image.
    /*public override Texture2D CustomFrame(CustomCardModel card)
    {
        //This will attempt to load CuteSakikoMod/images/cards/frame.png
        return PreloadManager.Cache.GetTexture2D("cards/frame.png".ImagePath());
    }*/

    //Color of small card icons
    public override Color DeckEntryCardColor => new("#21405e");

    public override bool IsColorless => false;
}