using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Others;

//Mostly utilities to get asset paths.

public static class StringExtensions
{
    public static string ImagePath(this string path)
    {
        return Path.Join(Entry.ModId, "images", path);
    }

    public static string CardImagePath(this string path)
    {
        return Path.Join(Entry.ModId, "images", "card_portraits", path);
    }

    public static string RestSiteIconPath(this string fileName)
    {
        return $"res://{Entry.ModId}/images/ui/rest_site/{fileName.Replace("\\", "/")}";
    }

    public static string ChordIconPath(this string fileName)
    {
        return $"res://CuteSakikoMod/images/ui/chords/{fileName}";
    }

    public static string BigCardImagePath(this string path)
    {
        return Path.Join(Entry.ModId, "images", "card_portraits", "big", path);
    }

    public static string PowerImagePath(this string path)
    {
        return Path.Join(Entry.ModId, "images", "powers", path);
    }

    public static string BigPowerImagePath(this string path)
    {
        return Path.Join(Entry.ModId, "images", "powers", "big", path);
    }

    public static string RelicImagePath(this string path)
    {
        return Path.Join(Entry.ModId, "images", "relics", path);
    }

    public static string BigRelicImagePath(this string path)
    {
        return Path.Join(Entry.ModId, "images", "relics", "big", path);
    }

    public static string CharacterUiPath(this string path)
    {
        return Path.Join(Entry.ModId, "images", "charui", path);
    }

    public static string PotionsImagePath(this string path)
    {
        return Path.Join(Entry.ModId, "images", "potions", path);
    }
}

public static class AssetHelper
{
    // 资产命名规则：直接用类名（PascalCase），不做 snake_case 转换
    //   例：AnonStrike → images/cards/AnonStrike.png
    // 之前的 snake 版本（anon_strike.png）已废弃，图片文件已同步改名。

    // 卡片
    public static CardAssetProfile CardAssetProfile(this CardModel card, CardVisualStyle? style = null)
    {
        var typeName = card.GetType().Name;

        // 如果没指定 style，根据稀有度自动推断：Ancient 稀有度 -> 先古样式，其他 -> 标准样式
        var finalStyle = style ?? (card.Rarity == CardRarity.Ancient 
            ? CardVisualStyle.Ancient 
            : CardVisualStyle.Standard);

        return new CardAssetProfile($"res://{Entry.ModId}/images/cards/{typeName}.png")
        {
            VisualStyle = finalStyle
        };
    }

    // 能力
    public static PowerAssetProfile PowerAssetProfile(this PowerModel power)
    {
        var typeName = power.GetType().Name;
        return new PowerAssetProfile(
            $"res://{Entry.ModId}/images/powers/{typeName}.png",
            $"res://{Entry.ModId}/images/powers/{typeName}.png"
        );
    }

    // 遗物
    public static RelicAssetProfile RelicAssetProfile(this RelicModel relic)
    {
        var typeName = relic.GetType().Name;
        return new RelicAssetProfile(
            $"res://{Entry.ModId}/images/relics/{typeName}.png",
            $"res://{Entry.ModId}/images/relics/{typeName}.png",
            $"res://{Entry.ModId}/images/relics/big/{typeName}.png"
        );
    }

    // 药水
    public static PotionAssetProfile PotionAssetProfile(this PotionModel potion)
    {
        var typeName = potion.GetType().Name;
        return new PotionAssetProfile(
            $"res://{Entry.ModId}/images/potions/{typeName}.png",
            $"res://{Entry.ModId}/images/potions/{typeName}.png"
        );
    }
}