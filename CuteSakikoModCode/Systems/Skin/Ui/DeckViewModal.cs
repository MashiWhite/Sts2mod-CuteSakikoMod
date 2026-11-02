using Godot;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Ui;

/// <summary>
///     卡组查看界面的根节点。
///     - 全屏半透明遮罩由 DeckPresetPanel 里的 ColorRect 负责
///     - 本类自身用 MouseFilter.Stop 拦截点击
///     - inspect 打开时自动隐藏，inspect 开始关闭时立即淡入
///     - 首次出现 / 重新出现 / 关闭都带渐变
///     尺寸与位置由外部（DeckPresetPanel.OpenDeckView）显式设置。
/// </summary>
public sealed partial class DeckViewModal : Control
{
    private const float FirstAppearDuration = 0.22f;
    private const float ReappearDuration = 0.10f; // inspect 关闭后重新出现，更早/更快
    private const float FadeOutDuration = 0.18f;
    private bool _closing;

    private Tween? _fadeTween;
    private bool _wasVisible = true;
    private bool _hotkeysBlocked;
    /// <summary>上一帧 inspect 是否开着。用于检测「inspect 关掉了」这个边沿。</summary>
    private bool _inspectWasOpen;

    public override void _Ready()
    {
        // 自己拦截：整块 modal 层不要漏给下层 UI
        MouseFilter = MouseFilterEnum.Stop;

        // 注意：不调用 SetAnchorsPreset，Size/Position 由外部显式设置

        Modulate = new Color(1, 1, 1, 0); // 初始全透明
        PlayFadeIn(FirstAppearDuration);

        // ★★ 挡住下层所有热键（关键修复，见下面 BlockLowerHotkeys 的注释）
        BlockLowerHotkeys();
    }

    public override void _ExitTree()
    {
        ReleaseLowerHotkeys();
    }

    // ─────────────────────────────────────────────────────────
    // 挡住下层屏幕的热键
    //
    //   ❌ 2026-11-02 用户反馈的真实 bug：
    //      在卡组查看页按 ESC，不仅关掉了弹窗，**角色选择界面也一起退了**（回到主菜单）。
    //   根因：角色选择界面的返回是用 NBackButton 注册到 NHotkeyManager 的热键
    //        （ui_cancel / mega_back / mega_pause_and_back 三个动作）。
    //        NHotkeyManager 有自己的 _UnhandledInput，`SetInputAsHandled()` 只能拦住
    //        **比它更晚处理**的节点；我们在 DeckViewModal 里调 SetInputAsHandled，
    //        并不能阻止同样在 _UnhandledInput 阶段跑的 NHotkeyManager 再响应一次。
    //        结果一次 ESC 被两边都吃到了。
    //   ✅ 原版为此专门提供了 NHotkeyManager.AddBlockingScreen(screen)：
    //        它会给全部 MegaInput.AllInputs 压一条空 action，压在当前绑定栈的最上面，
    //        而 NHotkeyManager 分发时取的是 Value.LastOrDefault() —— 也就是「最后加的那个」。
    //        所以只要我们的 modal 是最后 AddBlockingScreen 的，下面的返回热键就全哑了，
    //        而我们自己的 _UnhandledInput 仍能收到 ESC（那是直接读 InputEvent，不走热键表）。
    //
    //   ❌ 第二层坑（2026-11-02 用户复现）：
    //      「打开卡组 → 点一张牌看大图 → ESC 关掉看牌 → 再 ESC」→ 又会同时退两个界面！
    //      原因：原版 NInspectCardScreen.Open() 里也会
    //            PushHotkeyPressedBinding(MegaInput.cancel, Close)
    //            （NInspectCardScreen.cs:142-143），把**它自己的 Close 压到栈顶**，
    //            我们的 blocker 就被顶下去了。
    //            Close() 时它会 Remove 掉自己那条，看起来恢复了；但
    //            **RemoveBlockingScreen 是立刻执行的，而 Visible=false 被挂在 tween 里延迟 0.25s**，
    //            我们这边靠 SkinPanelVisibilityHelper 判断"inspect 还开着"会有时间差，
    //            一帧错位就可能让某次 ESC 落到下层。
    //   ✅ 解法：不依赖时序，改成**每帧检查 blocker 是否还在栈顶，不在就补压**。
    //      代价极小（一次字典查找），而且对任何会 AddBlockingScreen 的原版界面都免疫。
    //   ✅ 关闭时必须 RemoveBlockingScreen，否则角色选择界面的返回键从此永久失效。
    //      用 _ExitTree 兜底，不管怎么关（按钮 / ESC / 外部 QueueFree）都会释放。
    // ─────────────────────────────────────────────────────────
    private void BlockLowerHotkeys()
    {
        var manager = NHotkeyManager.Instance;
        if (manager == null || _hotkeysBlocked) return;

        manager.AddBlockingScreen(this);
        _hotkeysBlocked = true;
        GD.Print("[DeckViewModal] Lower-screen hotkeys blocked");
    }

    private void ReleaseLowerHotkeys()
    {
        if (!_hotkeysBlocked) return;
        _hotkeysBlocked = false;

        var manager = NHotkeyManager.Instance;
        if (manager == null) return;

        manager.RemoveBlockingScreen(this);
        GD.Print("[DeckViewModal] Lower-screen hotkeys released");
    }

    /// <summary>
    ///     inspect 关闭后把 blocker 重新压回栈顶。
    ///     原理：PushHotkeyPressedBinding 内部有 Contains 检查，重复 push 不会移动位置，
    ///     所以必须先 RemoveBlockingScreen 再 AddBlockingScreen，才能把它挪到 list 末尾。
    ///
    ///     ⚠️ NHotkeyManager 的实现细节（NHotkeyManager.cs:97-135）：
    ///       · AddBlockingScreen 用 `_blockingScreens.Add(screen, action)` —— **Add 不是索引赋值**，
    ///         同一个 screen 连续 Add 两次会抛 ArgumentException；
    ///       · RemoveBlockingScreen 靠字典里存的**那一个 delegate 实例**逐条删绑定，删完再移出字典。
    ///       ⇒ 必须严格 Remove → Add 配对。这里套 try/catch 兜住，
    ///         万一状态不同步也宁可重建，不让整块弹窗崩掉。
    /// </summary>
    private void RaiseBlockerToTop()
    {
        var manager = NHotkeyManager.Instance;
        if (manager == null) return;

        try
        {
            if (_hotkeysBlocked) manager.RemoveBlockingScreen(this);
            manager.AddBlockingScreen(this);
            _hotkeysBlocked = true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[DeckViewModal] RaiseBlockerToTop failed: {ex.Message}");
            try
            {
                manager.RemoveBlockingScreen(this);
                manager.AddBlockingScreen(this);
                _hotkeysBlocked = true;
            }
            catch (Exception ex2)
            {
                GD.PrintErr($"[DeckViewModal] RaiseBlockerToTop retry failed: {ex2.Message}");
                _hotkeysBlocked = false;
            }
        }
    }

    // ─────────────────────────────────────────────────────────
    // ESC / 手柄取消键 关掉弹窗
    //   ★ 主要拦截其实在 BlockLowerHotkeys（挡住下层角色选择界面的返回热键），
    //     这里负责「我们这个弹窗自己被 ESC 关掉」。
    //   ★ 用 _UnhandledInput 而不是 _Input：让 LineEdit 之类的先吃掉自己的输入；
    //     并且只有 Visible 时才响应，inspect 看牌时不应被这里抢走。
    //   ★ 动作名用原版 MegaInput 常量，不要手写字符串，改名了会一起断。
    //   ★ SetInputAsHandled 保留作为双保险，但**不能只靠它** ——
    //     它拦不住同样在 _UnhandledInput 阶段跑的 NHotkeyManager。
    // ─────────────────────────────────────────────────────────
    public override void _UnhandledInput(InputEvent @event)
    {
        if (_closing || !Visible) return;

        if (@event.IsActionPressed(MegaInput.cancel)
            || @event.IsActionPressed(MegaInput.back)
            || @event.IsActionPressed(MegaInput.pauseAndBack))
        {
            // inspect 开着时，ESC 应该先关 inspect，而不是把整个弹窗关掉
            if (SkinPanelVisibilityHelper.IsInspectScreenOpen()) return;

            CloseSelf();
            GetViewport()?.SetInputAsHandled();
        }
    }

    private void PlayFadeIn(float duration)
    {
        _fadeTween?.Kill();
        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(this, "modulate:a", 1.0f, duration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    /// <summary>带渐变地关闭自己（关闭按钮/外部调用都走这里）。</summary>
    public void CloseSelf()
    {
        if (_closing) return;
        _closing = true;

        _fadeTween?.Kill();
        _fadeTween = CreateTween();
        _fadeTween.TweenProperty(this, "modulate:a", 0.0f, FadeOutDuration)
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.In);
        _fadeTween.TweenCallback(Callable.From(QueueFree));
    }

    public override void _Process(double delta)
    {
        if (_closing) return;

        var inspectVisible = SkinPanelVisibilityHelper.IsInspectScreenOpen();

        // ★★ 边沿检测：inspect 从「开」变「关」的这一帧，把 blocker 重新压回栈顶。
        //    原因见 BlockLowerHotkeys 上面那段注释：原版 NInspectCardScreen.Open()
        //    会把**它自己的 Close 热键**压到栈顶（NInspectCardScreen.cs:142-143），
        //    我们的 blocker 被顶下去；它 Close() 时虽然会 Remove 自己那条，
        //    但 Visible=false 被挂在 tween 里延迟 0.25s，中间存在时序错位。
        //    不依赖原版时序，直接在这条边沿上补压一次，最稳。
        if (_inspectWasOpen && !inspectVisible)
            RaiseBlockerToTop();
        _inspectWasOpen = inspectVisible;

        var shouldBeVisible = !inspectVisible;

        if (_wasVisible == shouldBeVisible) return;
        _wasVisible = shouldBeVisible;

        if (shouldBeVisible)
        {
            // inspect 开始关闭 → 立即淡入，不再等 0.25s 淡出跑完
            Visible = true;
            Modulate = new Color(1, 1, 1, 0);
            PlayFadeIn(ReappearDuration);
        }
        else
        {
            // inspect 打开 → 立刻隐藏，避免层级打架
            _fadeTween?.Kill();
            Visible = false;
            Modulate = new Color(1, 1, 1);
        }
    }
}