using Godot;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Ui;

/// <summary>
///     皮肤 UI 面板（卡组切换 / 遗物切换 / 描述框）在不同角色选择界面上的定位入口。
///     标准选择、闪电战、恐惧模式的界面类型不同，InfoPanel 的节点名也不同
///     （标准/闪电战叫 InfoPanel，恐惧模式是我们复刻的 DreadInfoPanel）。
/// </summary>
public static class SkinScreenLocator
{
    public static Control? FindSelectScreen(Node from)
    {
        Node? n = from;
        while (n != null)
        {
            if (n is NCharacterSelectScreen) return (Control)n;
#if STS2_AT_LEAST_0_113_0
            if (n is MegaCrit.Sts2.Core.Nodes.Screens.Blitz.NBlitzScreen) return (Control)n;
            if (n is MegaCrit.Sts2.Core.Nodes.Screens.Dread.NDreadScreen) return (Control)n;
#endif
            n = n.GetParent();
        }

        return null;
    }

    /// <summary>标准 / 闪电战是 InfoPanel；恐惧模式用我们复刻的 DreadInfoPanel。</summary>
    public static Control? FindInfoPanel(Control screen)
    {
        return screen.GetNodeOrNull<Control>("DreadInfoPanel")
               ?? screen.GetNodeOrNull<Control>("InfoPanel");
    }

    /// <summary>InfoPanel 里的遗物框。</summary>
    public static Control? FindRelicBox(Control screen)
    {
        return FindInfoPanel(screen)?.GetNodeOrNull<Control>("VBoxContainer/Relic");
    }
}
