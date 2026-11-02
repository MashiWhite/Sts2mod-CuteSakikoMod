using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Core;
using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Networking;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Ui;

public sealed partial class SkinPreviewPanel : Control
{
    private Node2D? _currentVisuals;

    // ★ 逻辑可见性：inspect 打开时这个值不变，Visible 由 _Process 动态计算
    private bool _logicalVisible;
    private Label _nameLabel = null!;
    private SubViewport _viewport = null!;

    public override void _Ready()
    {
        GD.Print("[SkinPreviewPanel] _Ready");

        Size = new Vector2(380, 400);
        Position = new Vector2(1400, 200);
        MouseFilter = MouseFilterEnum.Pass;

        BuildUi();

        SkinSystemEvents.CurrentCharacterChanged += OnCurrentCharacterChanged;
        SkinSystemEvents.ArtSkinChanged += OnArtSkinChanged;
        RefreshAndToggle();
    }

    public override void _ExitTree()
    {
        SkinSystemEvents.CurrentCharacterChanged -= OnCurrentCharacterChanged;
        SkinSystemEvents.ArtSkinChanged -= OnArtSkinChanged;
    }

    // ★ 每帧检查 inspect 是否打开，动态调整自己的可见性
    public override void _Process(double delta)
    {
        var shouldBeVisible = _logicalVisible && !SkinPanelVisibilityHelper.IsInspectScreenOpen();
        if (Visible != shouldBeVisible)
            Visible = shouldBeVisible;
    }

    private void SetLogicalVisible(bool visible)
    {
        _logicalVisible = visible;
        Visible = visible && !SkinPanelVisibilityHelper.IsInspectScreenOpen();
    }

    private void BuildUi()
    {
        const float panelW = 380f;
        const float panelH = 400f;
        const float padX = 16f;
        const float padY = 16f;
        const float btnW = 48f;
        const float btnH = 64f;
        const float viewportW = 220f;
        const float viewportH = 320f;
        const float nameH = 28f;

        AddChild(MakePanelBackdrop(new Vector2(panelW, panelH)));

        var viewportX = padX + btnW + 8f;
        var viewportY = padY;

        var viewportContainer = new SubViewportContainer
        {
            Position = new Vector2(viewportX, viewportY),
            Size = new Vector2(viewportW, viewportH),
            Stretch = true,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(viewportContainer);

        _viewport = new SubViewport
        {
            TransparentBg = true,
            Size = new Vector2I((int)viewportW, (int)viewportH),
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always
        };
        viewportContainer.AddChild(_viewport);

        _viewport.AddChild(new Camera2D { Position = new Vector2(0, -160) });

        var btnY = viewportY + viewportH / 2 - btnH / 2;

        var prevBtn = MakeArrowButton("res://images/atlases/ui_atlas.sprites/settings_tiny_left_arrow.tres");
        prevBtn.Position = new Vector2(padX, btnY);
        prevBtn.Size = new Vector2(btnW, btnH);
        AddChild(prevBtn);
        prevBtn.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(_ => Shift(-1)));

        var nextBtnX = viewportX + viewportW + 8f;
        var nextBtn = MakeArrowButton("res://images/atlases/ui_atlas.sprites/settings_tiny_right_arrow.tres");
        nextBtn.Position = new Vector2(nextBtnX, btnY);
        nextBtn.Size = new Vector2(btnW, btnH);
        AddChild(nextBtn);
        nextBtn.Connect(NClickableControl.SignalName.Released, Callable.From<NClickableControl>(_ => Shift(1)));

        _nameLabel = new Label
        {
            Position = new Vector2(0, viewportY + viewportH + 8f),
            Size = new Vector2(panelW, nameH),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _nameLabel.AddThemeFontSizeOverride("font_size", 20);
        AddChild(_nameLabel);
    }

    // ─────────────────────────────────────────────────────────
    // 面板底板：与原版 InfoPanel 完全同款
    //   原版 scenes/screens/character_select_screen.tscn 的 InfoPanel/NinePatchRect 就是
    //   texture = images/ui/fuzzy_nine_patch_char_select.png + patch_margin 42 + modulate(0,0,0,0.5)，
    //   四个角的切角正是这张九宫格自带的形状。用同一张图、同一份 margin，
    //   切角尺寸与斜率就和原版一模一样，不再是我们自己画的直角 ColorRect。
    //   这里的 alpha 沿用面板原来的 0.55，只换形状不改压暗程度。
    // ─────────────────────────────────────────────────────────
    internal const string PanelBackdropTexture = "res://images/ui/fuzzy_nine_patch_char_select.png";
    private const int PanelBackdropPatchMargin = 42;

    internal static NinePatchRect MakePanelBackdrop(Vector2 size)
    {
        var bg = new NinePatchRect
        {
            Texture = ResourceLoader.Load<Texture2D>(PanelBackdropTexture),
            Position = Vector2.Zero,
            Size = size,
            PatchMarginLeft = PanelBackdropPatchMargin,
            PatchMarginTop = PanelBackdropPatchMargin,
            PatchMarginRight = PanelBackdropPatchMargin,
            PatchMarginBottom = PanelBackdropPatchMargin,
            // ★ 与原版一致：把白色底图整体乘成半透明黑
            Modulate = new Color(0, 0, 0, 0.55f),
            // ★ 底板不吃鼠标，点击要落到底下的按钮
            MouseFilter = MouseFilterEnum.Ignore
        };
        return bg;
    }

    // ─────────────────────────────────────────────────────────
    // 原版返回按钮（红底 + 奶白回箭头 + 金描边）
    //
    //   ❌ 2026-11-02 两次踩坑记录，别再走回头路：
    //     ① RitsuGodotNodeFactories.CreateFromScenePath<NBackButton>()
    //        → System.InvalidOperationException: No node factory registered for NBackButton
    //          RitsuLib 只给 NCreatureVisuals 这类认识它自己工厂的类型注册了 factory，
    //          NBackButton 没有。而且异常从 OpenDeckView 中途抛出，后面的代码全不执行，
    //          表现就是「弹窗卡死、没有返回键、ESC 也没反应」。
    //     ② PreloadManager.Cache.GetScene("res://scenes/ui/back_button.tscn")
    //        → 同样失败：这个场景不在 AssetCache 的预载集合里（日志：Asset not cached）。
    //
    //   ✅ 最终方案：不用原版场景，用原版**贴图**自己拼一个按钮（DeckViewBackButton : NButton），
    //      贴图来自原版图集（和 back_button.tscn 引用的是同一批，肉眼完全一致）：
    //        底     res://images/atlases/ui_atlas.sprites/back_button.tres
    //        箭头   res://images/atlases/compressed.sprites/back_button_arrow.tres
    //        描边   res://images/atlases/compressed.sprites/back_button_outline.tres
    //      hover 金色描边 / 按下变灰 / 音效全部照 NBackButton.cs 复刻（见 DeckViewBackButton）。
    //      位置完全由我们控制（原版那套 _showPos tween 只会在左下角，反而摆不到弹窗旁边）。
    //
    //   图层顺序（★ 这就是「只亮一圈边」的全部秘密，照抄原版 back_button.tscn）：
    //     Shadow  (黑，最底，错位偏移做出投影)
    //     Outline (金，加色，**比 Image 大一圈**，垫在 Image 下面)
    //     Image   (红底)
    //       └ Icon (奶白回箭头)
    //   阴影 → 金边 → 红底 三层叠起来，金边只在四周露出一圈，所以 hover 是「描边发光」而不是「整面发光」。
    //   ❌ 2026-11-02 踩过：小祥一开始把 Outline 加到 Image **上面**，而且几乎同尺寸，
    //      结果整面旗全被金色实心图盖住 → 「整个按钮都发光」，和原版完全不是一回事。
    //      贴图 back_button_outline.tres 是**整块实心旗形**（alpha 满、RGB 纯白），
    //      没有任何"只描边"的信息，全靠下面这层盖住中间。
    //
    //   几何完全照原版 back_button.tscn（★ 尺寸必须用原版的 200×110，不能自己改成 200×104）：
    //     容器 200×110
    //     Shadow  L-9  T-1  R+58 B+39
    //     Outline L-24 T-16 R+49 B+30   ← 每边比 Image 多 3px，露出的正好是一圈金边
    //     Image   L-21 T-13 R+46 B+27   → 实际 267×150
    //     Icon    在 Image 内绝对坐标 (88,28)-(168,108)，即 80×80
    //   ❌ 2026-11-02 踩过：容器高度从 110 改成 104，横向 offset 没变，
    //      Image 变成 267×144 → 红旗被横向拉长、看起来「扁扁的」；
    //      Icon 又用了 anchor 比例而不是原版的绝对 80×80，箭头位置也歪了。
    //      结论：这套 offset 是原版按 200×110 调好的，改高度必须整套等比换算，否则就别动。
    // ─────────────────────────────────────────────────────────
    internal const string BackButtonBgPath = "res://images/atlases/ui_atlas.sprites/back_button.tres";
    internal const string BackButtonArrowPath =
        "res://images/atlases/compressed.sprites/back_button_arrow.tres";
    internal const string BackButtonOutlinePath =
        "res://images/atlases/compressed.sprites/back_button_outline.tres";

    /// <summary>原版返回按钮的容器尺寸，几何 offset 都是按这个调的，别改。</summary>
    internal static readonly Vector2 BackButtonSize = new(200f, 110f);

    /// <summary>做一个卡组弹窗专用的原版同款返回按钮（hover 金边 / 按下变灰 / 原版音效）。</summary>
    internal static DeckViewBackButton MakeBackButton()
    {
        var btn = new DeckViewBackButton { Name = "DeckViewBackButton" };
        // 缩放要以中心为轴，否则 hover 放大时会往右下飘
        btn.PivotOffset = BackButtonSize / 2f;

        var bgTex = ResourceLoader.Load<Texture2D>(BackButtonBgPath);
        var outlineTex = ResourceLoader.Load<Texture2D>(BackButtonOutlinePath);

        // ── ① Shadow：黑色投影，垫在最底 ──
        var shadow = new TextureRect
        {
            Name = "Shadow",
            Texture = bgTex,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = new Color(0f, 0f, 0f, 0.25098f)
        };
        shadow.SetAnchorsPreset(LayoutPreset.FullRect);
        shadow.OffsetLeft = -9f;
        shadow.OffsetTop = -1f;
        shadow.OffsetRight = 58f;
        shadow.OffsetBottom = 39f;
        btn.AddChild(shadow);

        // ── ② Outline：金色加色描边，比 Image 大一圈，**在 Image 下面** ──
        //   原版挂 themes/canvas_item_material_additive_shared.tres（blend_mode = 1 加色）。
        //   平时 Modulate 全透明（交给 DeckViewBackButton.Init 置），hover 才淡入金色。
        var outline = new TextureRect
        {
            Name = "Outline",
            Texture = outlineTex,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore,
            Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add }
        };
        outline.SetAnchorsPreset(LayoutPreset.FullRect);
        outline.OffsetLeft = -24f;
        outline.OffsetTop = -16f;
        outline.OffsetRight = 49f;
        outline.OffsetBottom = 30f;
        btn.AddChild(outline);

        // ── ③ Image：红底 ──
        var image = new TextureRect
        {
            Name = "Image",
            Texture = bgTex,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore
        };
        image.SetAnchorsPreset(LayoutPreset.FullRect);
        image.OffsetLeft = -21f;
        image.OffsetTop = -13f;
        image.OffsetRight = 46f;
        image.OffsetBottom = 27f;
        btn.AddChild(image);

        // ── ④ Icon：奶白回箭头，挂在 Image 下，**绝对坐标 80×80**（照原版 layout_mode = 0） ──
        var arrow = new TextureRect
        {
            Name = "Icon",
            Texture = ResourceLoader.Load<Texture2D>(BackButtonArrowPath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = MouseFilterEnum.Ignore,
            Modulate = new Color(1f, 0.964706f, 0.886275f),
            Position = new Vector2(88f, 28f),
            Size = new Vector2(80f, 80f)
        };
        image.AddChild(arrow);

        // 把图层交给按钮，由它管理 hover / 按下动画
        btn.Init(image, outline);
        return btn;
    }

    internal static NGoldArrowButton MakeArrowButton(string texturePath)
    {
        var btn = new NGoldArrowButton { Size = new Vector2(48, 64) };

        var mat = new ShaderMaterial { Shader = ResourceLoader.Load<Shader>("res://shaders/hsv.gdshader") };
        mat.SetShaderParameter("h", 1.0f);
        mat.SetShaderParameter("s", 1.0f);
        mat.SetShaderParameter("v", 1.0f);

        var icon = new TextureRect
        {
            Name = "TextureRect",
            Texture = ResourceLoader.Load<Texture2D>(texturePath),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
            Material = mat,
            Position = new Vector2(0, 8),
            Size = new Vector2(48, 48)
        };
        btn.AddChild(icon);
        return btn;
    }

    private void OnCurrentCharacterChanged(Type? _)
    {
        RefreshAndToggle();
    }

    private void OnArtSkinChanged(Type _)
    {
        Refresh();
    }

    private void RefreshAndToggle()
    {
        var reg = SkinUiContext.CurrentRegistry;
        SetLogicalVisible(reg != null && reg.AllSkins.Count > 1);
        if (_logicalVisible) Refresh();
    }

    private void Shift(int delta)
    {
        if (SkinUiContext.CurrentCharacterType is not { } characterType) return;
        var reg = SkinUiContext.CurrentRegistry;
        if (reg == null) return;

        SkinResolver.ModifyLocalChoice(characterType, c =>
        {
            var count = reg.AllSkins.Count;
            c.ArtSkinIndex = (c.ArtSkinIndex + delta + count) % count;
            var newSkin = reg.AllSkins[c.ArtSkinIndex];
            c.DeckPresetIndex = Math.Min(c.DeckPresetIndex, newSkin.DeckPresets.Count - 1);
            c.RelicPresetIndex = Math.Min(c.RelicPresetIndex, newSkin.RelicPresets.Count - 1);
        });

        if (SkinUiContext.GetLobby(this) is { } lobby)
            SkinDataStore.ModifyLobbyChoice(lobby, characterType,
                c => c.ArtSkinIndex = SkinResolver.GetLocalChoice(characterType).ArtSkinIndex);
        else if (RunManager.Instance?.DebugOnlyGetState() != null) SkinSyncService.Broadcast(characterType);

        Refresh();
        SkinSystemEvents.RaiseArtSkinChanged(characterType);
    }

    private void Refresh()
    {
        if (SkinUiContext.CurrentCharacterType is not { } characterType) return;
        var skin = SkinResolver.GetLocalSkin(characterType);
        if (skin == null) return;

        _currentVisuals?.QueueFree();
        _currentVisuals = null;

        var packed = ResourceLoader.Load<PackedScene>(skin.Assets.VisualsPath);
        if (packed != null)
        {
            _currentVisuals = packed.Instantiate<Node2D>();
            _viewport.AddChild(_currentVisuals);
        }

        _nameLabel.Text = new LocString("characters", skin.DisplayNameKey).GetFormattedText();
    }
}