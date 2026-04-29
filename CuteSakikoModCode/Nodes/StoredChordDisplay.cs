using CuteSakikoMod.CuteSakikoModCode.Systems;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes;

public partial class StoredChordDisplay : Control
{
    private VBoxContainer _vbox;

    public override void _Ready()
    {
        _vbox = GetNode<VBoxContainer>("VBoxContainer");
        _vbox.MouseFilter = MouseFilterEnum.Ignore;
    }

    public void UpdateChords(List<string> chords, int multiplier)
    {
        // 清空现有子节点
        foreach (var child in _vbox.GetChildren())
            child.QueueFree();

        foreach (var chord in chords)
        {
            var tex = ChordDisplayHelper.GetChordTexture(chord);
            var tip = ChordDisplayHelper.GetChordHoverTip(chord, multiplier);

            if (tex != null)
            {
                var btn = new TextureButton();
                btn.TextureNormal = tex;
                btn.StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered;
                btn.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
                btn.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                btn.CustomMinimumSize = new Vector2(48, 48);
                btn.MouseFilter = MouseFilterEnum.Stop;
                btn.ButtonMask = 0;

                if (tip != null)
                {
                    btn.MouseEntered += () => OnChordMouseEntered(btn, tip);
                    btn.MouseExited += () => OnChordMouseExited(btn);
                }

                _vbox.AddChild(btn);
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

                _vbox.AddChild(label);
            }
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
        base._ExitTree();
    }
}