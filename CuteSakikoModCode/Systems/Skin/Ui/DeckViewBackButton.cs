using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Ui;

/// <summary>
///     卡组查看弹窗用的「原版同款」返回按钮。
///
///     为什么不用原版 NBackButton 场景，见 SkinPreviewPanel.MakeBackButton 的注释：
///       ① RitsuLib 没给 NBackButton 注册 node factory，CreateFromScenePath 直接抛异常；
///       ② back_button.tscn 不在 AssetCache 预载集合里，PreloadManager.Cache.GetScene 取不到；
///       ③ 就算取到，NBackButton._Ready 会禁用自己、Enable() 用 tween 把位置锁到屏幕左下角，
///          摆不到弹窗旁边。
///
///     所以这里用原版**贴图**自建，并把 NBackButton 的交互行为逐条复刻
///     （依据 anti\Sts2RecoverBeta\src\Core\Nodes\CommonUi\NBackButton.cs）：
///       · hover  ：放大到 1.05 + 金色描边淡入 0.05s
///       · 离开   ：0.5s Expo 缓出收回 1.0 + 金色描边淡出
///       · 按下   ：收回 1.0 + 底图变灰 0.25s + 描边淡出
///       · 音效   ：点击 ui_back / hover ui_hover（NButton 基类负责）
///     唯一的差别是不做原版那套 _showPos/_hidePos 滑入滑出 —— 那个位置由弹窗决定。
/// </summary>
public sealed partial class DeckViewBackButton : NButton
{
    // ── 与原版 NBackButton.cs 完全一致的常量 ──
    private static readonly Vector2 HoverScale = Vector2.One * 1.05f;
    private static readonly Vector2 DownScale = Vector2.One;

    /// <summary>原版 _outlineColor = new Color("F0B400")，hover 时描边颜色</summary>
    private static readonly Color OutlineColor = new(0.941176f, 0.705882f, 0f);

    /// <summary>原版 _outlineTransparentColor = new Color("FF000000")，全透明</summary>
    private static readonly Color OutlineTransparentColor = new(0f, 0f, 0f, 0f);

    /// <summary>原版 _downColor = Colors.Gray，按下时的底图色</summary>
    private static readonly Color DownColor = Colors.Gray;

    private const double HoverInDuration = 0.05;
    private const double HoverOutDuration = 0.5;
    private const double PressDuration = 0.25;

    // 与原版一致的音效
    protected override string ClickedSfx => "event:/sfx/ui/clicks/ui_back";
    protected override string HoveredSfx => "event:/sfx/ui/clicks/ui_hover";

    private TextureRect _outline = null!;
    private TextureRect _buttonImage = null!;

    private Tween? _hoverTween;

    /// <summary>
    ///     由工厂（SkinPreviewPanel.MakeBackButton）在 new 完之后调用，把三个图层塞进来。
    ///     分成 Init 而不是在 _Ready 里 GetNode，是因为这三个节点是代码建的、有先后顺序，
    ///     直接用字段传进来最稳，也避免命名冲突。
    /// </summary>
    internal void Init(TextureRect buttonImage, TextureRect outline)
    {
        _buttonImage = buttonImage;
        _outline = outline;
        // 初始：描边全透明（原版 OnEnable 里也是先把描边置透明）
        _outline.Modulate = OutlineTransparentColor;
        _buttonImage.Modulate = Colors.White;
    }

    public override void _Ready()
    {
        ConnectSignals();
        // ★ 原版的 NBackButton._Ready 会在这里自我禁用（它要滑出屏幕），
        //   我们不需要滑出，所以保持启用；但 ConnectSignals 必须调，否则 hover/点击都没反应。
        Enable();
    }

    protected override void OnFocus()
    {
        base.OnFocus(); // 播 hover 音效
        _hoverTween?.Kill();
        _hoverTween = CreateTween().SetParallel();
        _hoverTween.TweenProperty(this, "scale", HoverScale, HoverInDuration);
        _hoverTween.TweenProperty(_outline, "modulate", OutlineColor, HoverInDuration);
    }

    protected override void OnUnfocus()
    {
        _hoverTween?.Kill();
        _hoverTween = CreateTween().SetParallel();
        _hoverTween.TweenProperty(this, "scale", Vector2.One, HoverOutDuration)
            .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
        _hoverTween.TweenProperty(_outline, "modulate", OutlineTransparentColor, HoverOutDuration);
    }

    protected override void OnPress()
    {
        base.OnPress(); // 播 ui_back 音效
        _hoverTween?.Kill();
        _hoverTween = CreateTween().SetParallel();
        _hoverTween.TweenProperty(this, "scale", DownScale, PressDuration)
            .SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.Out);
        _hoverTween.TweenProperty(_buttonImage, "modulate", DownColor, PressDuration)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _hoverTween.TweenProperty(_outline, "modulate", OutlineTransparentColor, PressDuration)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
    }
}
