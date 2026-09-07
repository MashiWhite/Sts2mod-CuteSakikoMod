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

namespace CuteSakikoMod.CuteSakikoModCode.Nodes;

public partial class TogawaLoanButton : NButton
{
    private bool _canClick = true;
    private HoverTip _hoverTip;
    private LocString _titleLoc;
    private LocString _descLoc;
    private Player? _player;
    private int _clickCount = 0;              // 已点击次数（0~5）

    private static readonly int[] GoldAmounts = { 75, 60, 45, 30, 15 };

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
        _descLoc.Add("amount", (decimal)GoldAmounts[0]);

        // 初始化悬停提示
        _hoverTip = new HoverTip(_titleLoc, _descLoc);

        Visible = false;
    }

    public void ResetForNewVisit()
    {
        _clickCount = 0;
        _canClick = true;
        Enable();
        Modulate = Colors.White;
        UpdateHoverTip();
    }

    public void ShowButton()
    {
        Visible = true;
        if (_clickCount >= GoldAmounts.Length)
        {
            Disable();
            Modulate = new Color(0.1f, 0.1f, 0.1f);
            _canClick = false;
        }
        else
        {
            Enable();
            Modulate = Colors.White;
            _canClick = true;
        }
    }

    public void HideButton()
    {
        Visible = false;
    }

    protected override void OnFocus()
    {
        base.OnFocus();
        UpdateHoverTip();   // 确保描述中的金币数最新

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
        if (_player == null || !_canClick || _clickCount >= GoldAmounts.Length) return;
        DoLoan();
    }

    private async void DoLoan()
    {
        _canClick = false;
        Disable();

        // 点击反馈：变灰
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate", new Color(0.1f, 0.1f, 0.1f), 0.2f)
            .SetEase(Tween.EaseType.Out);
        await ToSignal(tween, Tween.SignalName.Finished);

        try
        {
            int gold = GoldAmounts[_clickCount];
            await PlayerCmd.GainGold(gold, _player);

            var debtCard = _player.RunState.CreateCard<Debt>(_player);
            var addResult = await CardPileCmd.Add(debtCard, PileType.Deck);
            CardCmd.PreviewCardPileAdd(addResult, 1.5f);

            _clickCount++;

            if (_clickCount >= GoldAmounts.Length)
            {
                // 全部用完，永久禁用并变黑
                Disable();
                Modulate = new Color(0.1f, 0.1f, 0.1f);
                _canClick = false;
                UpdateHoverTip();   // 更新显示为0金币或保持
            }
            else
            {
                Enable();
                Modulate = Colors.White;
                _canClick = true;
                UpdateHoverTip();
            }
        }
        catch (Exception e)
        {
            GD.PrintErr($"[TogawaLoan] {e}");
            if (_clickCount < GoldAmounts.Length)
            {
                Enable();
                Modulate = Colors.White;
                _canClick = true;
            }
        }
    }

    /// <summary>更新悬停提示中的金币变量并重建 HoverTip</summary>
    private void UpdateHoverTip()
    {
        int gold = _clickCount < GoldAmounts.Length ? GoldAmounts[_clickCount] : 0;
        _descLoc.Add("amount", (decimal)gold);   // 更新变量
        _hoverTip = new HoverTip(_titleLoc, _descLoc);  // 重新生成 HoverTip
    }
}