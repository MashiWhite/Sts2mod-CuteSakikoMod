using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using System.Reflection;
using STS2RitsuLib.Scaffolding.Characters; // ModCharacterTemplate<,,>

namespace CuteSakikoMod.CuteSakikoModCode.Nodes;

public partial class VakuuMoeButton : NButton
{
    private AncientEventModel? _eventModel;

    public override void _Ready()
    {
        ConnectSignals();

        // 通过反射获取事件模型
        var eventRoom = NEventRoom.Instance;
        if (eventRoom != null)
        {
            var eventField = typeof(NEventRoom).GetField("_event",
                BindingFlags.NonPublic | BindingFlags.Instance);
            _eventModel = eventField?.GetValue(eventRoom) as AncientEventModel;
        }

        // 只有角色继承自 ModCharacterTemplate<,,> 时才显示按钮
        if (!IsModCharacter(_eventModel?.Owner?.Character))
        {
            QueueFree();
            return;
        }

        // 按钮尺寸和位置（与贷款按钮完全一致）
        float buttonWidth = 180f;
        float buttonHeight = 180f;

        AnchorLeft = 1.0f;
        AnchorRight = 1.0f;
        AnchorTop = 0.0f;
        AnchorBottom = 0.0f;

        OffsetRight = 0;
        OffsetLeft = -buttonWidth;
        OffsetTop = 750;                     // 可自行微调 Y 轴
        OffsetBottom = OffsetTop + buttonHeight;

        // 加载图标
        var texture = GD.Load<Texture2D>("res://CuteSakikoMod/images/others/others/vakuu_love_icon.png");
        var img = new TextureRect();
        img.Texture = texture;
        img.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        img.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        img.MouseFilter = MouseFilterEnum.Ignore;

        img.AnchorRight = 1.0f;
        img.AnchorBottom = 1.0f;
        img.OffsetRight = 0;
        img.OffsetLeft = 0;
        img.OffsetTop = 0;
        img.OffsetBottom = 0;
        AddChild(img);
    }

    /// <summary>
    /// 检测角色是否派生自 ModCharacterTemplate<,,> 泛型基类
    /// </summary>
    private static bool IsModCharacter(CharacterModel? character)
    {
        if (character == null) return false;
        var type = character.GetType();
        var target = typeof(ModCharacterTemplate<,,>);
        while (type != null && type != typeof(CharacterModel))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == target)
                return true;
            type = type.BaseType;
        }
        return false;
    }

    protected override void OnFocus()
    {
        base.OnFocus();

        var title = new LocString("events", "CUTE_SAKIKO_MOD_VAKUU_BUTTON.title");
        var desc = new LocString("events", "CUTE_SAKIKO_MOD_VAKUU_BUTTON.description");
        var tip = new HoverTip(title, desc);

        var tipSet = NHoverTipSet.CreateAndShow(this, tip);
        if (tipSet == null) return;

        var alignment = GlobalPosition.X > GetViewportRect().Size.X * 0.6f
            ? HoverTipAlignment.Left
            : HoverTipAlignment.Right;
        tipSet.SetAlignment(this, alignment);

        var viewportRect = GetViewportRect();
        Vector2 pos = tipSet.GlobalPosition;
        if (pos.X < 0) pos.X = 10;
        if (pos.X + tipSet.Size.X > viewportRect.Size.X)
            pos.X = viewportRect.Size.X - tipSet.Size.X - 10;
        if (pos.Y < 0) pos.Y = 10;
        if (pos.Y + tipSet.Size.Y > viewportRect.Size.Y)
            pos.Y = viewportRect.Size.Y - tipSet.Size.Y - 10;
        tipSet.GlobalPosition = pos;
    }

    protected override void OnUnfocus()
    {
        NHoverTipSet.Remove(this);
        base.OnUnfocus();
    }

    protected override void OnRelease()
    {
        base.OnRelease();
        if (_eventModel == null) return;

        var player = _eventModel.Owner;
        if (player == null) return;

        var creature = player.Creature;
        creature.SetMaxHpInternal(creature.MaxHp + 5);
        creature.SetCurrentHpInternal(creature.CurrentHp + 5);

        // 通过反射调用 protected 方法 Done()
        var doneMethod = typeof(AncientEventModel).GetMethod("Done",
            BindingFlags.NonPublic | BindingFlags.Instance);
        doneMethod?.Invoke(_eventModel, null);
    }
}