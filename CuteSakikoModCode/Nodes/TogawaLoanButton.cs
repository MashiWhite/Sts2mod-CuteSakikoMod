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
    private bool _usedThisVisit;
    private HoverTip _hoverTip;

    public override void _Ready()
    {
        ConnectSignals();

        var room = NMerchantRoom.Instance;
        _player = room?.Room.GetLocalInventory()?.Player;

        float buttonWidth = 180f;
        float buttonHeight = 180f;

        AnchorLeft = 1.0f;
        AnchorRight = 1.0f;
        AnchorTop = 0.0f;
        AnchorBottom = 0.0f;

        OffsetRight = 0;
        OffsetLeft = -buttonWidth;
        OffsetTop = 750;
        OffsetBottom = OffsetTop + buttonHeight;

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

        var title = new LocString("events", "TOGAWA_GROUP.title");
        var desc = new LocString("events", "TOGAWA_GROUP.description");
        _hoverTip = new HoverTip(title, desc);

        Visible = false;
    }

    /// <summary> 每次进入商店房间时调用，重置贷款状态 </summary>
    public void ResetForNewVisit()
    {
        _usedThisVisit = false;
        _canClick = true;
        if (!IsEnabled)
            Enable();
        Modulate = Colors.White;
        // 注意：不设置 Visible，由 ShowButton 控制
    }

    public void ShowButton()
    {
        // 如果本次已贷款，直接隐藏不显示
        if (_usedThisVisit)
        {
            Visible = false;
            return;
        }

        Visible = true;
        if (!IsEnabled)
            Enable();
        Modulate = Colors.White;
        _canClick = true;
    }

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
        if (_player == null || _usedThisVisit || !_canClick) return;
        DoLoan();
    }

    private async void DoLoan()
    {
        _canClick = false;
        _usedThisVisit = true;
        Disable();

        // ★ 点击反馈：立即变灰，不再弹回
        var originalModulate = Modulate;
        var greyColor = new Color(0.1f, 0.1f, 0.1f);
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", greyColor, 0.2f)
             .SetEase(Tween.EaseType.Out);
        await ToSignal(tween, Tween.SignalName.Finished);

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
            // 操作完成后直接隐藏按钮（本商店不再出现）
            Visible = false;
        }
    }
}