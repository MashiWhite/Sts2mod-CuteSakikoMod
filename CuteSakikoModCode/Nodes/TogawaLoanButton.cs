using System;
using System.Collections.Generic;
using CuteSakikoMod.CuteSakikoModCode.Others.Telemetry;
using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes;

public partial class TogawaLoanButton : NButton
{
    private bool _canClick = true;
    private LocString _descLoc;
    private HoverTip _hoverTip;
    private Player? _player;
    private LocString _titleLoc;

    public override void _Ready()
    {
        ConnectSignals();

        var room = NMerchantRoom.Instance;
        _player = room?.Room.GetLocalInventory()?.Player;

        // 按钮布局
        var buttonWidth = 180f;
        var buttonHeight = 180f;
        AnchorLeft = 1.0f;
        AnchorRight = 1.0f;
        AnchorTop = 0.0f;
        AnchorBottom = 0.0f;
        OffsetRight = 0;
        OffsetLeft = -buttonWidth;
        OffsetTop = 750;
        OffsetBottom = OffsetTop + buttonHeight;

        // 图标
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

        // 本地化字符串
        _titleLoc = new LocString("events", "CUTE_SAKIKO_MOD_TOGAWA_GROUP.title");
        _descLoc = new LocString("events", "CUTE_SAKIKO_MOD_TOGAWA_GROUP.description");
        UpdateHoverTip();

        Visible = false;
    }

    public void ResetForNewVisit()
    {
        _canClick = true;
        UpdateVisualState();
    }

    public void ShowButton()
    {
        Visible = true;
        UpdateVisualState();
    }

    public void HideButton()
    {
        Visible = false;
    }

    private void UpdateVisualState()
    {
        if (_player == null)
        {
            Disable();
            _canClick = false;
            UpdateHoverTip();
            return;
        }

        var relic = _player.GetRelic<TgwCard>();
        bool canClick = relic == null || relic.CanLoan(_player);

        if (canClick)
        {
            Enable();
            Modulate = Colors.White;
            _canClick = true;
        }
        else
        {
            Disable();
            Modulate = new Color(0.1f, 0.1f, 0.1f);
            _canClick = false;
        }

        UpdateHoverTip();
    }

    protected override void OnFocus()
    {
        base.OnFocus();
        UpdateHoverTip();

        var tips = new List<IHoverTip>
        {
            _hoverTip,
        };

        var tipSet = NHoverTipSet.CreateAndShow(this, tips);
        if (tipSet == null) return;

        var alignment = GlobalPosition.X > GetViewportRect().Size.X * 0.6f
            ? HoverTipAlignment.Left
            : HoverTipAlignment.Right;
        tipSet.SetAlignment(this, alignment);

        var viewportRect = GetViewportRect();
        var pos = tipSet.GlobalPosition;
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
        DoLoan();
    }

    private async void DoLoan()
{
    _canClick = false;
    Disable();

    var tween = CreateTween();
    tween.TweenProperty(this, "modulate", new Color(0.1f, 0.1f, 0.1f), 0.2f)
        .SetEase(Tween.EaseType.Out);
    await ToSignal(tween, Tween.SignalName.Finished);

    try
    {
        var player = _player!;
        var relic = player.GetRelic<TgwCard>();

        string loanKind;
        int principal;
        int debt;
        int level;

        if (relic == null)
        {
            // 首次：给遗物，借本金，欠应还总额
            var obtained = await RelicCmd.Obtain<TgwCard>(player);
            if (obtained == null) return;

            loanKind = "first";
            principal = obtained.LoanPrincipal;
            debt = obtained.LoanDebt;
            level = obtained.Level;

            obtained.Debt = debt;
            obtained.MarkLoaned(player);

            obtained.SetLoaning(true);
            try { await PlayerCmd.GainGold(principal, player); }
            finally { obtained.SetLoaning(false); }
            obtained.Flash();
        }
        else
        {
            if (!relic.CanLoan(player)) return;

            loanKind = "repeat";
            principal = relic.LoanPrincipal;
            debt = relic.LoanDebt;
            level = relic.Level;

            relic.Debt = debt;
            relic.MarkLoaned(player);

            relic.SetLoaning(true);
            try { await PlayerCmd.GainGold(principal, player); }
            finally { relic.SetLoaning(false); }
            relic.Flash();
        }

        // 遥测：贷款事件
        try
        {
            CuteSakikoModTelemetry.CaptureRoomButtonClicked(
                "shop", "togawa_loan", player.Character.Id.Entry,
                CuteSakikoModTelemetry.GetCurrentFloor(player),
                new Dictionary<string, object?>
                {
                    ["loan_kind"] = loanKind,
                    ["principal"] = principal,
                    ["debt"] = debt,
                    ["level"] = level
                });
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[TogawaLoan] Telemetry failed: {ex}");
        }
    }
    catch (Exception e)
    {
        GD.PrintErr($"[TogawaLoan] {e}");
    }
    finally
    {
        UpdateVisualState();
    }
}

    private void UpdateHoverTip()
    {
        var relic = _player?.GetRelic<TgwCard>();
        int amount = relic == null
            ? 50
            : (relic.Debt > 0 ? 0 : relic.LoanPrincipal);
        _descLoc.Add("amount", amount);
        _hoverTip = new HoverTip(_titleLoc, _descLoc);
    }
}