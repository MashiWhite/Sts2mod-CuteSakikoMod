#if STS2_AT_LEAST_0_113_0
using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Core;
using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Ui;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.Blitz;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.Dread;

namespace CuteSakikoMod.CuteSakikoModCode.Patches.Skin;

/// <summary>
///     闪电战（Blitz）角色选择界面：把当前角色喂给皮肤 UI，并同步大厅皮肤数据。
///     原版皮肤补丁只挂 NCharacterSelectScreen，Blitz/Dread 用的是各自的 NSubmenu。
/// </summary>
[HarmonyPatch(typeof(NBlitzScreen), nameof(NBlitzScreen.SelectCharacter))]
public static class SkinSelectCharacterBlitzPatch
{
    private static void Postfix(NCharacterSelectButton charSelectButton)
    {
        var type = charSelectButton.Character.GetType();
        SkinUiContext.SetCurrentCharacter(CharacterSkinRegistry.HasSkins(type) ? type : null);
    }
}

[HarmonyPatch(typeof(NBlitzScreen), nameof(NBlitzScreen.OnSubmenuOpened))]
public static class SkinBlitzSubmenuOpenedPatch
{
    private static void Postfix(NBlitzScreen __instance)
    {
        var lobby = __instance.Lobby;
        if (lobby == null)
        {
            SkinUiContext.SetCurrentCharacter(null);
            return;
        }

        SkinResolver.SyncFromLobby(lobby, true);

        var type = lobby.LocalPlayer.character?.GetType();
        SkinUiContext.SetCurrentCharacter(type != null && CharacterSkinRegistry.HasSkins(type) ? type : null);
    }
}

/// <summary>恐惧模式（Dread）角色选择界面：同上。</summary>
[HarmonyPatch(typeof(NDreadScreen), nameof(NDreadScreen.SelectCharacter))]
public static class SkinSelectCharacterDreadPatch
{
    private static void Postfix(NCharacterSelectButton charSelectButton)
    {
        var type = charSelectButton.Character.GetType();
        SkinUiContext.SetCurrentCharacter(CharacterSkinRegistry.HasSkins(type) ? type : null);
    }
}

[HarmonyPatch(typeof(NDreadScreen), nameof(NDreadScreen.OnSubmenuOpened))]
public static class SkinDreadSubmenuOpenedPatch
{
    private static void Postfix(NDreadScreen __instance)
    {
        var lobby = __instance.Lobby;
        if (lobby == null)
        {
            SkinUiContext.SetCurrentCharacter(null);
            return;
        }

        SkinResolver.SyncFromLobby(lobby, true);

        var type = lobby.LocalPlayer.character?.GetType();
        SkinUiContext.SetCurrentCharacter(type != null && CharacterSkinRegistry.HasSkins(type) ? type : null);
    }
}
#endif
