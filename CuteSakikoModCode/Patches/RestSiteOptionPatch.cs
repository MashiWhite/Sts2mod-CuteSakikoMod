using CuteSakikoMod.CuteSakikoModCode.Events;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(RestSiteOption))]
public static class RestSiteOptionPatch
{
    [HarmonyPatch(typeof(RestSiteOption), "Icon", MethodType.Getter)]
    [HarmonyPrefix]
    public static bool IconPrefix(RestSiteOption __instance, ref Texture2D __result)
    {
        if (__instance is PracticeGuitarOption)
        {
            var iconPath = "practice_guitar.png".RestSiteIconPath();
            __result = PreloadManager.Cache.GetTexture2D(iconPath);
            if (__result == null)
                // 如果自定义图片加载失败，返回一个空白纹理避免后续报错
                __result = new Texture2D();
            return false;
        }

        return true;
    }

    [HarmonyPatch(nameof(RestSiteOption.Title), MethodType.Getter)]
    [HarmonyPrefix]
    public static void TitlePostfix(RestSiteOption __instance, ref LocString __result)
    {
        if (__instance is PracticeGuitarOption) __result = new LocString("rest_site_ui", "OPTION_PRACTICE_GUITAR.name");
    }
}