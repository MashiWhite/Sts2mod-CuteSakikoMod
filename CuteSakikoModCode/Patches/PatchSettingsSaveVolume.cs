using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;
using CuteSakikoMod.CuteSakikoModCode.Systems;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(SettingsSave), "set_VolumeMaster")]
public static class PatchSettingsSaveVolumeMaster
{
    static void Postfix()
    {
        AudioManager.RefreshMusicVolume();
    }
}

[HarmonyPatch(typeof(SettingsSave), "set_VolumeBgm")]
public static class PatchSettingsSaveVolumeBgm
{
    static void Postfix()
    {
        AudioManager.RefreshMusicVolume();
    }
}