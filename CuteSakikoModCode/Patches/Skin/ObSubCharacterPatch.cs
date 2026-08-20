using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace CuteSakikoMod.CuteSakikoModCode.Patches.Skin;

[HarmonyPatch(typeof(NCharacterSelectScreen), "OnSubmenuOpened")]
public static class ObPopupSetupPatch
{
    static void Postfix(NCharacterSelectScreen __instance)
    {
        ObPopupHelper.Setup(__instance);
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), "SelectCharacter")]
public static class ObPopupShowOnSakiSelectPatch
{
    static void Postfix(
        NCharacterSelectScreen __instance,
        NCharacterSelectButton charSelectButton,
        CharacterModel characterModel)
    {
        if (characterModel is CuteSaki)
        {
            ObPopupHelper.OnSakiSelected();
        }
        else if (characterModel is CuteOb)
        {
            ObPopupHelper.OnObSelected();
        }
        else
        {
            ObPopupHelper.OnOtherCharacterSelected();
        }
    }
}

[HarmonyPatch(typeof(NCharacterSelectScreen), "OnSubmenuClosed")]
public static class ObPopupCleanupPatch
{
    static void Postfix(NCharacterSelectScreen __instance)
    {
        ObPopupHelper.Cleanup(__instance);
    }
}