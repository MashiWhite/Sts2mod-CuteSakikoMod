using CuteSakikoMod.CuteSakikoModCode.Systems;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes;

public partial class ChordSlotDropTarget : Control
{
    private ChordCategory _slotCategory;
    private int _slotIndex;          // 当前槽位的索引（0-based）
    private string _currentChordId;
    private ChordManagementScreen _screen;

    public ChordSlotDropTarget(ChordCategory slotCategory, int slotIndex, string currentChordId, ChordManagementScreen screen)
    {
        _slotCategory = slotCategory;
        _slotIndex = slotIndex;
        _currentChordId = currentChordId;
        _screen = screen;
        CustomMinimumSize = new Vector2(80, 80);
        MouseFilter = Control.MouseFilterEnum.Pass;

        SetDragForwarding(
            default(Callable),
            Callable.From((Vector2 atPosition, Variant data) => CanDropImpl(atPosition, data)),
            Callable.From((Vector2 atPosition, Variant data) => DropImpl(atPosition, data))
        );

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;

        if (!string.IsNullOrEmpty(currentChordId))
        {
            var texture = ChordDisplayHelper.GetChordTexture(currentChordId);
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
        }
    }

    private bool CanDropImpl(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.String) return false;
        string chordId = (string)data;
        if (!ChordManager.AllChords.TryGetValue(chordId, out var def)) return false;
        if (_slotCategory == ChordCategory.Bonus) return true;
        return def.Category == _slotCategory;
    }

    private void DropImpl(Vector2 atPosition, Variant data)
    {
        if (_screen.Guitar == null || _screen._readOnly) return;
        string newChordId = (string)data;

        // 直接使用构造函数传入的槽位索引
        _screen.SetTempSlot(_slotCategory, _slotIndex, newChordId);
    }

    private void OnMouseEntered()
    {
        if (!string.IsNullOrEmpty(_currentChordId) && _screen?.Guitar?.Owner?.Creature != null)
        {
            var tip = ChordDisplayHelper.GetDynamicChordHoverTip(
                _currentChordId,
                _screen.Guitar.Owner.Creature,
                _screen.Guitar.GetDisplayBonus());
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