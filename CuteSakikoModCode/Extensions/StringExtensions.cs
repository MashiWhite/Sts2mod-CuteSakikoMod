using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Audio;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Extensions;

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
    public static string GetSnakeCaseName(Type type)
    {
        var name = type.Name;
        var snake = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1_$2").ToLower();
        return snake;
    }

    // 卡片
    public static CardAssetProfile CardAssetProfile(this CardModel card)
    {
        var snake = GetSnakeCaseName(card.GetType());
        return new CardAssetProfile($"res://{Entry.ModId}/images/cards/{snake}.png");
    }

    // 能力
    public static PowerAssetProfile PowerAssetProfile(this PowerModel power)
    {
        var snake = GetSnakeCaseName(power.GetType());
        return new PowerAssetProfile(
            $"res://{Entry.ModId}/images/powers/{snake}.png",
            $"res://{Entry.ModId}/images/powers/big/{snake}.png"
        );
    }

    // 遗物
    public static RelicAssetProfile RelicAssetProfile(this RelicModel relic)
    {
        var snake = GetSnakeCaseName(relic.GetType());
        return new RelicAssetProfile(
            $"res://{Entry.ModId}/images/relics/{snake}.png",
            $"res://{Entry.ModId}/images/relics/{snake}_outline.png",
            $"res://{Entry.ModId}/images/relics/big/{snake}.png"
        );
    }

    // 药水
    public static PotionAssetProfile PotionAssetProfile(this PotionModel potion)
    {
        var snake = GetSnakeCaseName(potion.GetType());
        return new PotionAssetProfile(
            $"res://{Entry.ModId}/images/potions/{snake}.png",
            $"res://{Entry.ModId}/images/potions/{snake}_outline.png"
        );
    }

    public static class AudioManager
    {
        /// <summary>
        ///     根据游戏主音量和音效音量，自动计算最终音量并播放指定的音频文件。
        /// </summary>
        /// <param name="filePath">音频文件的完整路径</param>
        /// <param name="baseVolume">基础音量 (通常在 0.0 到 1.0 之间)</param>
        public static void PlaySound(string filePath, float baseVolume = 1.0f)
        {
            // 安全获取音量设置，如果存档系统还没初始化则使用默认值
            var settings = SaveManager.Instance?.SettingsSave;
            var masterVol = settings?.VolumeMaster ?? 1.0f;
            var sfxVol = settings?.VolumeSfx ?? 1.0f;
            var finalVol = Mathf.Clamp(baseVolume * masterVol * sfxVol, 0.0f, 1.0f);

            FmodStudioStreamingFiles.TryPlaySoundFile(filePath, finalVol);
        }
    }
}