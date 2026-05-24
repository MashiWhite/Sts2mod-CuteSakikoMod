using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using System.Collections.Generic;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes;

public partial class TogawaLoanButton : NButton
{
    private Player? _player;
    private bool _canClick = true;
    private HoverTip _hoverTip;

    public override void _Ready()
    {
        ConnectSignals();

        var room = NMerchantRoom.Instance;
        _player = room?.Room.GetLocalInventory()?.Player;

        // 按钮固定尺寸
        float buttonWidth = 180f;
        float buttonHeight = 180f;

        // 锚点：右边缘 + 顶部
        AnchorLeft = 1.0f;
        AnchorRight = 1.0f;
        AnchorTop = 0.0f;
        AnchorBottom = 0.0f;

        OffsetRight = 0;
        OffsetLeft = -buttonWidth;
        OffsetTop = 750;                     // ★ 你的 Y 轴位置
        OffsetBottom = OffsetTop + buttonHeight;

        // 创建头像
        var texture = GD.Load<Texture2D>("res://CuteSakikoMod/images/others/others/togawa_group_icon.png");
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

        // 悬停提示文本
        var title = new LocString("events", "TOGAWA_GROUP.title");
        var desc = new LocString("events", "TOGAWA_GROUP.description");
        _hoverTip = new HoverTip(title, desc);

        // 初始隐藏
        Visible = false;
    }

    public void ShowButton() => Visible = true;
    public void HideButton() => Visible = false;

    protected override void OnFocus()
    {
        base.OnFocus();

        var tips = new List<IHoverTip>
        {
            _hoverTip,
            HoverTipFactory.FromCard<Debt>()
        };

        var tipSet = NHoverTipSet.CreateAndShow(this, tips);
        if (tipSet == null) return;

        var alignment = GlobalPosition.X > GetViewportRect().Size.X * 0.6f
            ? HoverTipAlignment.Left
            : HoverTipAlignment.Right;
        tipSet.SetAlignment(this, alignment);

        // 边界修正
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
        if (_player == null || !_canClick) return;
        PlayClickAnimation();
        DoLoan();
    }

    private void PlayClickAnimation()
    {
        var originalModulate = Modulate;
        var pressedColor = new Color(originalModulate.R * 0.6f, originalModulate.G * 0.6f, originalModulate.B * 0.6f);
        Modulate = pressedColor;

        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", originalModulate, 0.15f)
             .SetEase(Tween.EaseType.Out)
             .SetTrans(Tween.TransitionType.Back);
    }

    private async void DoLoan()
    {
        _canClick = false;
        try
        {
            await PlayerCmd.GainGold(50, _player);
            var debtCard = _player.RunState.CreateCard<Debt>(_player);
            var addResult = await CardPileCmd.Add(debtCard, PileType.Deck);
            CardCmd.PreviewCardPileAdd(addResult, time: 1.5f);
        }
        catch (System.Exception e)
        {
            GD.PrintErr($"[TogawaLoan] {e}");
        }
        finally
        {
            _canClick = true;
        }
    }
}