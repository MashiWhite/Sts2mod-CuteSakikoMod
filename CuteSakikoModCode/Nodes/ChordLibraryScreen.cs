using CuteSakikoMod.CuteSakikoModCode.Systems;
using Godot;
using MegaCrit.Sts2.Core.Nodes;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes;

public partial class ChordLibraryScreen : Control
{
    private static ChordLibraryScreen _browseInstance;

    private readonly Dictionary<string, ChordButton> _chordButtons = new();
    private bool _isCancelled;
    private bool _isSelectMode;
    private List<string> _selectedChords;

    private TaskCompletionSource<List<string>> _selectionTcs;
    private int _targetCount;

    public static void OpenBrowse()
    {
        if (_browseInstance != null && IsInstanceValid(_browseInstance))
            return;
        _browseInstance = new ChordLibraryScreen();
        _browseInstance._isSelectMode = false;
        _browseInstance.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _browseInstance.MouseFilter = MouseFilterEnum.Stop;
        NRun.Instance.GlobalUi.AddChild(_browseInstance);
        _browseInstance.TreeExited += () => _browseInstance = null;
    }

    public async Task<List<string>> ShowSelection(int count)
    {
        if (_selectionTcs != null && !_selectionTcs.Task.IsCompleted)
            _selectionTcs.TrySetCanceled();

        _selectionTcs = new TaskCompletionSource<List<string>>();
        _selectedChords = new List<string>();
        _targetCount = count;
        _isSelectMode = true;
        _isCancelled = false;

        if (!IsInsideTree())
        {
            SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            NRun.Instance.GlobalUi.AddChild(this);
        }

        return await _selectionTcs.Task;
    }

    public override void _Ready()
    {
        var parent = GetParent();
        parent?.MoveChild(this, 0);

        // ===== 背景 =====
        var bg = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.6f),
            MouseFilter = _isSelectMode ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop
        };
        // 锚定为全屏
        bg.AnchorLeft = 0;
        bg.AnchorRight = 1;
        bg.AnchorTop = 0;
        bg.AnchorBottom = 1;
        // 上下留出安全区域（相对于屏幕高度取固定值）
        var topMargin = 80f;
        float bottomMargin = 0;
        bg.OffsetTop = topMargin;
        bg.OffsetBottom = -bottomMargin; // 负值表示从底部往上缩进
        bg.OffsetLeft = 0;
        bg.OffsetRight = 0;
        // 非选择模式下点击背景关闭
        if (!_isSelectMode)
            bg.GuiInput += e =>
            {
                if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
                    QueueFree();
            };
        AddChild(bg);

        // ===== 顶部栏 =====
        var topBar = new ColorRect
        {
            Color = new Color(0.15f, 0.1f, 0.2f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        // 锚定到顶部，与背景同宽（通过锚点左右拉伸，再偏移）
        topBar.AnchorLeft = 0;
        topBar.AnchorRight = 1;
        topBar.AnchorTop = 0;
        topBar.AnchorBottom = 0; // 高度由 offset 决定
        topBar.OffsetTop = topMargin; // 与背景顶部对齐
        topBar.OffsetBottom = topMargin + 60f; // 固定高度 60
        AddChild(topBar);

        var titleLabel = new Label
        {
            Text = _isSelectMode ? $"选择 {_targetCount} 个和弦 (已选 0)" : "和弦图鉴",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        titleLabel.AnchorLeft = 0;
        titleLabel.AnchorRight = 1;
        titleLabel.AnchorTop = 0;
        titleLabel.AnchorBottom = 1;
        titleLabel.AddThemeFontSizeOverride("font_size", 24);
        titleLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
        topBar.AddChild(titleLabel);

        // ===== 滚动区域 =====
        var scroll = new ScrollContainer();
        // 锚定到背景内部
        scroll.AnchorLeft = 0;
        scroll.AnchorRight = 1;
        scroll.AnchorTop = 0;
        scroll.AnchorBottom = 1;
        // 左右边距：屏幕宽度的 15% 或最小 200px 用来避免边缘太挤
        var leftMargin = Mathf.Max(200f, GetViewportRect().Size.X * 0.15f);
        var rightMargin = -Mathf.Max(40f, GetViewportRect().Size.X * 0.05f); // 负值
        scroll.OffsetLeft = leftMargin;
        scroll.OffsetRight = rightMargin;
        // 上下边距：位于 topBar 下方、背景底部上方
        scroll.OffsetTop = topBar.OffsetBottom + 20f; // 与顶栏间隔 20
        scroll.OffsetBottom = bg.OffsetBottom + 20f; // bg.OffsetBottom 是负值，+20 就是距离底部 20
        AddChild(scroll);

        // ===== 内容容器 =====
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 24);
        scroll.AddChild(vbox);

        var categories = new[]
        {
            ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant,
            ChordCategory.Anon, ChordCategory.Bonus
        };

        foreach (var cat in categories)
        {
            var chords = ChordManager.AllChordsList.Where(c => c.Category == cat).ToList();
            if (chords.Count == 0) continue;

            var catLabel = new Label { Text = GetCategoryDisplayName(cat) };
            catLabel.AddThemeFontSizeOverride("font_size", 20);
            catLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
            vbox.AddChild(catLabel);

            var grid = new GridContainer();
            // 动态列数：根据可用宽度计算
            var availableWidth = scroll.Size.X - 40f; // 预留内部边距
            var colWidth = 120f; // 每个按钮最小宽度
            var columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / colWidth));
            grid.Columns = columns;
            foreach (var chordDef in chords)
            {
                var btn = new ChordButton();
                btn.Setup(chordDef.Id);
                btn.Modulate = _isSelectMode && _selectedChords.Contains(chordDef.Id)
                    ? new Color(1, 1, 0.5f)
                    : Colors.White;

                if (_isSelectMode)
                {
                    var chordId = chordDef.Id;
                    btn.Pressed += () => OnChordButtonPressed(chordId, titleLabel);
                }

                _chordButtons[chordDef.Id] = btn;
                grid.AddChild(btn);
            }

            vbox.AddChild(grid);
        }

        // 监听窗口大小变化，重新调整布局
        GetViewport().SizeChanged += OnViewportSizeChanged;
    }

    private void OnViewportSizeChanged()
    {
        // 重新计算滚动区域的边距和网格列数
        var leftMargin = Mathf.Max(200f, GetViewportRect().Size.X * 0.15f);
        var rightMargin = -Mathf.Max(40f, GetViewportRect().Size.X * 0.05f);

        var scroll = FindChild("ScrollContainer") as ScrollContainer; // 如果你给 scroll 加了名字可以这样找
        // 或者直接在 _Ready 中保存引用
        // 这里简化：遍历子节点找到 ScrollContainer (略)
        // 更好的做法：在类中保存 scroll 引用
    }

    private void OnChordButtonPressed(string chordId, Label titleLabel)
    {
        if (!_isSelectMode) return;

        if (_selectedChords.Contains(chordId))
        {
            _selectedChords.Remove(chordId);
            if (_chordButtons.TryGetValue(chordId, out var btn))
                btn.Modulate = Colors.White;
        }
        else
        {
            if (_selectedChords.Count >= _targetCount) return;
            _selectedChords.Add(chordId);
            if (_chordButtons.TryGetValue(chordId, out var btn))
                btn.Modulate = new Color(1, 1, 0.5f);
        }

        titleLabel.Text = $"选择 {_targetCount} 个和弦 (已选 {_selectedChords.Count})";

        if (_selectedChords.Count == _targetCount)
        {
            _selectionTcs?.TrySetResult(_selectedChords.ToList());
            QueueFree();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
        {
            if (_isSelectMode)
            {
                _isCancelled = true;
                _selectionTcs?.TrySetResult(new List<string>());
            }

            QueueFree();
            AcceptEvent();
        }

        if (!_isSelectMode && @event is InputEventMouseButton mouse
                           && mouse.ButtonIndex == MouseButton.Right && mouse.Pressed)
        {
            QueueFree();
            AcceptEvent();
        }
    }

    public override void _ExitTree()
    {
        if (_isSelectMode && _selectionTcs != null && !_selectionTcs.Task.IsCompleted)
            _selectionTcs.TrySetResult(new List<string>());
    }

    private static string GetCategoryDisplayName(ChordCategory cat)
    {
        return cat switch
        {
            ChordCategory.Major => "大三和弦",
            ChordCategory.Minor => "小三和弦",
            ChordCategory.Dominant => "属七和弦",
            ChordCategory.Anon => "爱音和弦",
            ChordCategory.Bonus => "额外和弦",
            _ => "其他和弦"
        };
    }
}