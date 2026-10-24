using CuteSakikoMod.CuteSakikoModCode.Others.Telemetry;
using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes.SakiButton;

public partial class TogawaLoanButton : ModActionButton
{
    private LocString _descLoc = null!;
    private HoverTip _hoverTip;
    private Player? _player;
    private TgwCard? _subscribedRelic;
    private LocString _titleLoc = null!;

    protected override void OnInit()
    {
        var room = NMerchantRoom.Instance;
        _player = room?.Room.GetLocalInventory()?.Player;

        // 调用基类自适应布局
        ApplyCustomLayout(180f, 180f);

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

        TrySubscribe();
        Visible = false;
    }

    public void ResetForNewVisit()
    {
        TrySubscribe();
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

    protected override bool CanTrigger
    {
        get
        {
            if (_player == null) return false;
            var relic = _player.GetRelic<TgwCard>();
            return relic == null || relic.CanLoan(_player);
        }
    }

    private void TrySubscribe()
    {
        if (_player == null) return;

        var relic = _player.GetRelic<TgwCard>();
        if (ReferenceEquals(relic, _subscribedRelic)) return;

        Unsubscribe();
        if (relic == null) return;

        _subscribedRelic = relic;
        relic.DisplayAmountChanged += OnRelicChanged;
        relic.StatusChanged += OnRelicChanged;
    }

    private void Unsubscribe()
    {
        if (_subscribedRelic == null) return;
        _subscribedRelic.DisplayAmountChanged -= OnRelicChanged;
        _subscribedRelic.StatusChanged -= OnRelicChanged;
        _subscribedRelic = null;
    }

    private void OnRelicChanged()
    {
        UpdateVisualState();
    }

    public void RefreshState()
    {
        TrySubscribe();
        UpdateVisualState();
    }

    protected override void ExecuteAction()
    {
        if (_player == null) return;
        DoLoan();
    }

    private void UpdateVisualState()
    {
        if (_player == null)
        {
            Disable();
            UpdateHoverTip();
            return;
        }

        if (CanTrigger)
        {
            Enable();
            Modulate = Colors.White;
        }
        else
        {
            Disable();
            Modulate = new Color(0.1f, 0.1f, 0.1f);
        }

        UpdateHoverTip();
    }

    protected override void OnFocus()
    {
        base.OnFocus();
        UpdateHoverTip();

        var tips = new List<IHoverTip> { _hoverTip };
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

    private async void DoLoan()
    {
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
                TrySubscribe();
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

    public override void _ExitTree()
    {
        Unsubscribe();
        base._ExitTree();
    }
}