using CuteSakikoMod.CuteSakikoModCode.Others.Config;
using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes.SakiButton;

public abstract partial class ModActionButton : NButton
{
    private enum State { Idle, Pressing, WaitingDouble }

    // ★ 新增：全局位置变更事件
    public static event Action? PositionChanged;
    public static void NotifyPositionChanged() => PositionChanged?.Invoke();

    private State _state = State.Idle;
    private float _pressTimer;
    private float _doubleTimer;
    private ColorRect? _progressOverlay;

    // ★ 新增：缓存当前按钮的尺寸，以便刷新时使用
    private float _btnWidth = 180f;
    private float _btnHeight = 180f;

    protected abstract bool CanTrigger { get; }
    protected abstract void ExecuteAction();

    protected virtual void OnInit() { }

    public sealed override void _Ready()
    {
        ConnectSignals();
        CreateProgressOverlay();
        OnInit();

        if (_progressOverlay != null)
            MoveChild(_progressOverlay, GetChildCount() - 1);

        // ★ 订阅事件
        PositionChanged += UpdateLayout;
    }

    public override void _ExitTree()
    {
        // ★ 取消订阅
        PositionChanged -= UpdateLayout;
        base._ExitTree();
    }

    // ★ 新增：当收到通知时重新排版
    private void UpdateLayout()
    {
        if (IsInsideTree() && !IsQueuedForDeletion())
        {
            ApplyCustomLayout(_btnWidth, _btnHeight);
        }
    }

    /// <summary>
    /// 贴右边缘，并允许垂直位置 (Y轴) 根据用户设置动态居中。
    /// </summary>
    protected void ApplyCustomLayout(float buttonWidth = 180f, float buttonHeight = 180f)
    {
        // 缓存尺寸
        _btnWidth = buttonWidth;
        _btnHeight = buttonHeight;

        // 固定 X 轴：贴最右侧边缘
        AnchorLeft = 1.0f;
        AnchorRight = 1.0f;
        OffsetLeft = -buttonWidth;
        OffsetRight = 0f;

        // 动态 Y 轴：根据配置的百分比定位，中心点对齐
        var y = ModConfig.ButtonPositionY;
        AnchorTop = y;
        AnchorBottom = y;
        OffsetTop = -buttonHeight / 2f;
        OffsetBottom = buttonHeight / 2f;
    }

    private void CreateProgressOverlay()
    {
        _progressOverlay = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.45f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false,
            AnchorLeft = 0,
            AnchorTop = 0,
            AnchorRight = 0,
            AnchorBottom = 0,
            Position = Vector2.Zero,
            Size = Vector2.Zero
        };
        AddChild(_progressOverlay);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_state == State.WaitingDouble)
        {
            _doubleTimer -= (float)delta;
            if (_doubleTimer <= 0)
            {
                // 双击模式下超时 → 放弃，不触发
                ResetPressState();
            }
        }

        UpdateLongPress((float)delta);
    }

    private void UpdateLongPress(float delta)
    {
        if (_state != State.Pressing) return;

        if (!CanTrigger)
        {
            ResetPressState();
            return;
        }

        _pressTimer += delta;
        var duration = Mathf.Max(0.01f, ModConfig.LongPressDuration);
        var progress = Mathf.Clamp(_pressTimer / duration, 0f, 1f);

        if (_progressOverlay != null)
        {
            _progressOverlay.Visible = true;
            _progressOverlay.Position = Vector2.Zero;
            _progressOverlay.Size = new Vector2(Size.X * progress, Size.Y);
        }

        if (_pressTimer >= duration)
        {
            ResetPressState();
            TryExecute();
        }
    }

    protected override void OnPress()
    {
        base.OnPress();

        if (!CanTrigger) return;

        if (ModConfig.ButtonClickMode == ButtonClickMode.LongPress)
        {
            _state = State.Pressing;
            _pressTimer = 0f;
            if (_progressOverlay != null)
            {
                _progressOverlay.Visible = true;
                _progressOverlay.Position = Vector2.Zero;
                _progressOverlay.Size = new Vector2(0, Size.Y);
            }
        }
    }

    protected override void OnRelease()
    {
        base.OnRelease();

        if (!CanTrigger)
        {
            ResetPressState();
            return;
        }

        switch (ModConfig.ButtonClickMode)
        {
            case ButtonClickMode.SingleClick:
                TryExecute();
                break;

            case ButtonClickMode.DoubleClick:
                if (_state == State.WaitingDouble)
                {
                    _state = State.Idle;
                    TryExecute();
                }
                else
                {
                    _state = State.WaitingDouble;
                    _doubleTimer = ModConfig.DoubleClickInterval;
                }
                break;

            case ButtonClickMode.LongPress:
                ResetPressState();
                break;
        }
    }

    private void ResetPressState()
    {
        _state = State.Idle;
        _pressTimer = 0f;
        _doubleTimer = 0f;
        if (_progressOverlay != null)
        {
            _progressOverlay.Visible = false;
            _progressOverlay.Position = Vector2.Zero;
            _progressOverlay.Size = Vector2.Zero;
        }
    }

    private void TryExecute()
    {
        if (!CanTrigger) return;
        ExecuteAction();
    }
}