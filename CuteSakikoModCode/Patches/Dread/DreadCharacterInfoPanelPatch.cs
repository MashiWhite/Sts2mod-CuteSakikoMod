#if STS2_AT_LEAST_0_113_0
using CuteSakikoMod.CuteSakikoModCode.Systems.Dread;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;

namespace CuteSakikoMod.CuteSakikoModCode.Patches.Dread;

/// <summary>
///     恐惧模式没有原版角色描述框：选中角色时把原版 InfoPanel 复制过来并刷新内容。
/// </summary>
[HarmonyPatch(typeof(NDreadScreen), nameof(NDreadScreen.SelectCharacter))]
public static class DreadInfoPanelSelectPatch
{
    private static void Postfix(NDreadScreen __instance, NCharacterSelectButton charSelectButton)
    {
        var panel = DreadCharacterInfoPanel.EnsureAttached(__instance);
        if (panel == null) return;
        DreadCharacterInfoPanel.Bind(charSelectButton.Character);
    }
}

[HarmonyPatch(typeof(NDreadScreen), nameof(NDreadScreen.OnSubmenuOpened))]
public static class DreadInfoPanelOpenedPatch
{
    private static void Postfix(NDreadScreen __instance)
    {
        var panel = DreadCharacterInfoPanel.EnsureAttached(__instance);
        if (panel == null) return;
        DreadCharacterInfoPanel.Bind(__instance.Lobby?.LocalPlayer.character);
    }
}
#endif
