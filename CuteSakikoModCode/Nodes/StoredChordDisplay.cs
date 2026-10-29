using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes;

public partial class StoredChordDisplay : Control
{
    /// <summary>单个和弦图标占位高度/宽度（与下面 CustomMinimumSize 保持一致）。</summary>
    private const float SlotSize = 48f;

    /// <summary>两列之间的横向间距。</summary>
    private const float ColumnGap = 4f;

    /// <summary>
    ///     场景里 VBoxContainer 的默认高度（stored_chord_display.tscn 的 offset_bottom = 172）。
    ///     单列最多放 floor(172 / 48) = 3 个，再往上就会向下压到角色的格挡数值。
    /// </summary>
    private const float BaseBoxHeight = 172f;

    private static readonly int MaxPerColumn = Mathf.Max(1, (int)(BaseBoxHeight / SlotSize));

    /// <summary>第一列（场景自带）。</summary>
    private VBoxContainer _vbox;

    /// <summary>第二列（容纳上限被提高时才创建，放在第一列左边）。</summary>
    private VBoxContainer? _vboxLeft;

    public override void _Ready()
    {
        _vbox = GetNode<VBoxContainer>("VBoxContainer");
        _vbox.MouseFilter = MouseFilterEnum.Ignore;
    }

    public void UpdateChords(List<string> chords, int bonus)
    {
        ClearColumn(_vbox);

        // 超出单列容量时**在左边新增一列**（而不是向下延申压住格挡数值、也不是整体平移）
        var hasOverflow = chords.Count > MaxPerColumn;
        EnsureLeftColumn(hasOverflow);

        if (_vboxLeft != null) ClearColumn(_vboxLeft);

        for (var i = 0; i < chords.Count; i++)
        {
            var target = i < MaxPerColumn ? _vbox : _vboxLeft;
            if (target == null) break;

            AddChordVisual(target, chords[i], bonus);
        }
    }

    private void EnsureLeftColumn(bool needed)
    {
        if (!needed || _vboxLeft != null) return;

        _vboxLeft = new VBoxContainer
        {
            // 放在第一列左侧，避免与角色脚下的格挡数值、上方的音符图标重叠
            Position = new Vector2(-(SlotSize + ColumnGap), 0f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(_vboxLeft);
    }

    private static void ClearColumn(VBoxContainer column)
    {
        // 清空现有子节点（QueueFree 是延迟的，先 RemoveChild 立刻摘掉，避免同帧新旧重叠）
        foreach (var child in column.GetChildren())
        {
            column.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void AddChordVisual(VBoxContainer column, string chord, int bonus)
    {
        var tex = ChordDisplayHelper.GetChordTexture(chord);
        var tip = ChordDisplayHelper.GetChordHoverTip(chord, bonus);

        if (tex != null)
        {
            var btn = new TextureButton();
            btn.TextureNormal = tex;
            btn.StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered;
            btn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
            btn.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            btn.CustomMinimumSize = new Vector2(SlotSize, SlotSize);
            btn.MouseFilter = MouseFilterEnum.Stop;
            btn.ButtonMask = 0;

            if (tip != null)
            {
                btn.MouseEntered += () => OnChordMouseEntered(btn, tip);
                btn.MouseExited += () => OnChordMouseExited(btn);
            }

            column.AddChild(btn);
        }
        else
        {
            var label = new Label();
            label.Text = chord;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.MouseFilter = MouseFilterEnum.Stop;

            if (tip != null)
            {
                label.MouseEntered += () => OnChordMouseEntered(label, tip);
                label.MouseExited += () => OnChordMouseExited(label);
            }

            column.AddChild(label);
        }
    }

    private void OnChordMouseEntered(Control anchor, HoverTip tip)
    {
        var alignment = HoverTip.GetHoverTipAlignment(anchor, 0.5f);
        NHoverTipSet.CreateAndShow(anchor, tip, alignment);
    }

    private void OnChordMouseExited(Control anchor)
    {
        NHoverTipSet.Remove(anchor);
    }

    public override void _ExitTree()
    {
        foreach (Control child in _vbox.GetChildren()) NHoverTipSet.Remove(child);
        if (_vboxLeft != null)
            foreach (Control child in _vboxLeft.GetChildren()) NHoverTipSet.Remove(child);
        base._ExitTree();
    }
}
