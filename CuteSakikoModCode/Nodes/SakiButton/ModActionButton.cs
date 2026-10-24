using CuteSakikoMod.CuteSakikoModCode.Others.Config;
using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes.SakiButton;

public abstract partial class ModActionButton : NButton
{
    private enum State { Idle, Pressing, WaitingDouble }

    // 遮罩 shader：用图标 alpha 作蒙版，按 UV.x 从左到右填充
    private const string OverlayShaderCode = @"
shader_type canvas_item;

uniform float progress : hint_range(0.0, 1.0) = 0.0;
uniform vec4 tint : source_color = vec4(0.0, 0.0, 0.0, 0.55);

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    float alphaMask = tex.a;
    float fill = 1.0 - smoothstep(progress - 0.02, progress + 0.02, UV.x);
    COLOR = vec4(tint.rgb, tint.a * alphaMask * fill);
}
";

    private State _state = State.Idle;
    private float _pressTimer;
    private float _doubleTimer;
    private Control? _progressOverlay;
    private ShaderMaterial? _overlayMaterial;

    protected abstract bool CanTrigger { get; }
    protected abstract void ExecuteAction();

    /// <summary>子类在这里做初始化（布局、图标、本地化等）。不要重写 _Ready。</summary>
    protected virtual void OnInit() { }

    /// <summary>
    /// 返回按钮图标纹理，用作长按遮罩的 alpha 蒙版。
    /// 返回 null 则回退到圆角矩形遮罩。
    /// </summary>
    protected virtual Texture2D? GetMaskTexture() => null;

    public sealed override void _Ready()
    {
        // 绝对不能调 base._Ready()：NButton 会检测运行时类型并抛异常。
        ConnectSignals();
        OnInit();

        // 子类可能在 OnInit 里 QueueFree（例如不满足显示条件）
        if (IsQueuedForDeletion()) return;

        CreateProgressOverlay();

        if (_progressOverlay != null)
            MoveChild(_progressOverlay, GetChildCount() - 1);
    }

    private void CreateProgressOverlay()
    {
        var maskTexture = GetMaskTexture();

        if (maskTexture != null)
        {
            var shader = new Shader { Code = OverlayShaderCode };
            _overlayMaterial = new ShaderMaterial { Shader = shader };

            var texRect = new TextureRect
            {
                Texture = maskTexture,
                Material = _overlayMaterial,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Visible = false,
                AnchorLeft = 0,
                AnchorTop = 0,
                AnchorRight = 1,
                AnchorBottom = 1,
                OffsetLeft = 0,
                OffsetRight = 0,
                OffsetTop = 0,
                OffsetBottom = 0
            };
            AddChild(texRect);
            _progressOverlay = texRect;
        }
        else
        {
            var panel = new Panel
            {
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Visible = false,
                AnchorLeft = 0,
                AnchorTop = 0,
                AnchorRight = 0,
                AnchorBottom = 0,
                Position = Vector2.Zero,
                Size = Vector2.Zero
            };

            var style = new StyleBoxFlat
            {
                BgColor = new Color(0, 0, 0, 0.45f),
                CornerRadiusTopLeft = 8,
                CornerRadiusTopRight = 8,
                CornerRadiusBottomLeft = 8,
                CornerRadiusBottomRight = 8
            };
            panel.AddThemeStyleboxOverride("panel", style);

            AddChild(panel);
            _progressOverlay = panel;
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (_state == State.WaitingDouble)
        {
            _doubleTimer -= (float)delta;
            if (_doubleTimer <= 0)
            {
                // 双击模式超时 → 放弃，不触发
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

        ApplyProgress(progress);

        if (_pressTimer >= duration)
        {
            ResetPressState();
            TryExecute();
        }
    }

    private void ApplyProgress(float progress)
    {
        if (_progressOverlay == null) return;

        if (_overlayMaterial != null)
        {
            _overlayMaterial.SetShaderParameter("progress", progress);
            _progressOverlay.Visible = true;
        }
        else if (_progressOverlay is Panel panel)
        {
            panel.Visible = true;
            panel.Position = Vector2.Zero;
            panel.Size = new Vector2(Size.X * progress, Size.Y);
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
            ApplyProgress(0f);
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

        if (_overlayMaterial != null)
            _overlayMaterial.SetShaderParameter("progress", 0f);

        if (_progressOverlay != null)
        {
            _progressOverlay.Visible = false;
            if (_progressOverlay is Panel panel)
            {
                panel.Position = Vector2.Zero;
                panel.Size = Vector2.Zero;
            }
        }
    }

    private void TryExecute()
    {
        if (!CanTrigger) return;
        ExecuteAction();
    }
}