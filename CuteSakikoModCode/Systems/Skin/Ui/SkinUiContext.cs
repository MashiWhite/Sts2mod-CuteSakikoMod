using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Core;
using Godot;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Ui;

public static class SkinUiContext
{
    public static Type? CurrentCharacterType { get; private set; }

    public static ICharacterSkinRegistry? CurrentRegistry
        => CurrentCharacterType is { } t ? CharacterSkinRegistry.ForCharacter(t) : null;

    public static void SetCurrentCharacter(Type? characterType)
    {
        if (CurrentCharacterType == characterType) return;
        CurrentCharacterType = characterType;
        SkinSystemEvents.RaiseCurrentCharacterChanged(characterType);
    }

    public static StartRunLobby? GetLobby(Node node)
    {
        var n = node;
        while (n != null)
        {
            if (n is NCharacterSelectScreen select) return select.Lobby;
#if STS2_AT_LEAST_0_113_0
            // 闪电战 / 恐惧模式：角色选择界面是 NBlitzScreen / NDreadScreen，同样能提供 Lobby
            if (n is MegaCrit.Sts2.Core.Nodes.Screens.Blitz.NBlitzScreen blitz) return blitz.Lobby;
            if (n is MegaCrit.Sts2.Core.Nodes.Screens.Dread.NDreadScreen dread) return dread.Lobby;
#endif
            n = n.GetParent();
        }
        return null;
    }
}