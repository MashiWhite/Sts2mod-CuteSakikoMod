namespace CuteSakikoMod.CuteSakikoModCode.Extensions;

//Mostly utilities to get asset paths.
public static class StringExtensions
{
    public static string ImagePath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", path);
    }

    public static string CardImagePath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", "card_portraits", path);
    }
    
    public static string RestSiteIconPath(this string fileName)
    {
        return $"res://{MainFile.ModId}/images/ui/rest_site/{fileName.Replace("\\", "/")}";
    }
    
    public static string ChordIconPath(this string fileName)
    {
        return $"res://CuteSakikoMod/images/ui/chords/{fileName}";
    }
    
    public static string BigCardImagePath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", "card_portraits", "big", path);
    }

    public static string PowerImagePath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", "powers", path);
    }

    public static string BigPowerImagePath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", "powers", "big", path);
    }

    public static string RelicImagePath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", "relics", path);
    }

    public static string BigRelicImagePath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", "relics", "big", path);
    }

    public static string CharacterUiPath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", "charui", path);
    }

    public static string PotionsImagePath(this string path)
    {
        return Path.Join(MainFile.ModId, "images", "potions", path);
    }
}