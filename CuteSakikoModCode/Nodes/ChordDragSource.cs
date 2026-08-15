using CuteSakikoMod.CuteSakikoModCode.Systems;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes;

public partial class ChordDragSource : Control
{
    private string _chordId;
    private ChordManagementScreen _screen;

    public ChordDragSource(string chordId, ChordManagementScreen screen)
    {
        _chordId = chordId;
        _screen = screen;
        CustomMinimumSize = new Vector2(80, 80);
        MouseFilter = Control.MouseFilterEnum.Pass;

        SetDragForwarding(
            Callable.From((Vector2 atPosition) => OnGetDragData(atPosition)),
            default(Callable),
            default(Callable)
        );

        var texture = ChordDisplayHelper.GetChordTexture(chordId);
        if (texture != null)
        {
            var img = new TextureRect
            {
                Texture = texture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            img.SetAnchorsPreset(LayoutPreset.FullRect);
            AddChild(img);
        }

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

    private Variant OnGetDragData(Vector2 atPosition)
    {
        var preview = new TextureRect
        {
            Texture = ChordDisplayHelper.GetChordTexture(_chordId),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            CustomMinimumSize = new Vector2(80, 80),
            Modulate = new Color(1, 1, 1, 0.8f)
        };
        SetDragPreview(preview);
        return _chordId;
    }

    private void OnMouseEntered()
    {
        if (_screen?.Guitar?.Owner?.Creature != null)
        {
            var tip = ChordDisplayHelper.GetDynamicChordHoverTip(
                _chordId,
                _screen.Guitar.Owner.Creature,
                _screen.Guitar.GetDisplayBonus()); // 使用 GetTotalBonus
            var tipSet = NHoverTipSet.CreateAndShow(this, tip);
            if (tipSet == null) return;

            var alignment = GlobalPosition.X > GetViewportRect().Size.X * 0.5f
                ? HoverTipAlignment.Left
                : HoverTipAlignment.Right;
            tipSet.SetAlignment(this, alignment);

            var pos = tipSet.GlobalPosition;
            var viewportRect = GetViewportRect();
            if (pos.X < 10) pos.X = 10;
            if (pos.X + tipSet.Size.X > viewportRect.Size.X - 10)
                pos.X = viewportRect.Size.X - tipSet.Size.X - 10;
            if (pos.Y < 10) pos.Y = 10;
            if (pos.Y + tipSet.Size.Y > viewportRect.Size.Y - 10)
                pos.Y = viewportRect.Size.Y - tipSet.Size.Y - 10;
            tipSet.GlobalPosition = pos;
        }
    }

    private void OnMouseExited()
    {
        NHoverTipSet.Remove(this);
    }
}