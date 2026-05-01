using CuteSakikoMod.CuteSakikoModCode.Systems;
using Godot;
using MegaCrit.Sts2.Core.Nodes;


namespace CuteSakikoMod.CuteSakikoModCode.Nodes;

public partial class ChordLibraryScreen : Control
{
    private static ChordLibraryScreen _browseInstance;

    private Dictionary<string, ChordButton> _chordButtons = new();

    private TaskCompletionSource<List<string>> _selectionTcs;
    private List<string> _selectedChords;
    private int _targetCount;
    private bool _isSelectMode;
    private bool _isCancelled;

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

        // 背景
        var bg = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.6f),
            MouseFilter = _isSelectMode ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop
        };
        bg.AnchorLeft = 0;
        bg.AnchorRight = 1;
        bg.AnchorTop = 0;
        bg.AnchorBottom = 1;
        bg.OffsetTop = 80;
        bg.OffsetBottom = 200;
        if (!_isSelectMode)
        {
            bg.GuiInput += e =>
            {
                if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
                    QueueFree();
            };
        }
        AddChild(bg);

        // 顶部栏
        var topBar = new ColorRect
        {
            Color = new Color(0.15f, 0.1f, 0.2f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        topBar.AnchorLeft = 0;
        topBar.AnchorRight = 1;
        topBar.Position = new Vector2(0, 80);
        topBar.Size = new Vector2(GetViewportRect().Size.X, 60);
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

        // 滚动区域
        var scroll = new ScrollContainer();
        scroll.AnchorLeft = 0;
        scroll.AnchorRight = 1;
        scroll.AnchorTop = 0;
        scroll.AnchorBottom = 1;
        scroll.OffsetLeft = 800;
        scroll.OffsetTop = 200;
        scroll.OffsetRight = -40;
        scroll.OffsetBottom = -20;
        AddChild(scroll);

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

            var grid = new GridContainer { Columns = 6 };
            foreach (var chordDef in chords)
            {
                var btn = new ChordButton();
                btn.Setup(chordDef.Id);
                btn.Modulate = _isSelectMode && _selectedChords.Contains(chordDef.Id)
                    ? new Color(1, 1, 0.5f) : Colors.White;

                if (_isSelectMode)
                {
                    var chordId = chordDef.Id;
                    btn.Pressed += () => OnChordButtonPressed(chordId, titleLabel);
                }

                // 将按钮存入字典，方便快速修改颜色
                _chordButtons[chordDef.Id] = btn;
                grid.AddChild(btn);
            }
            vbox.AddChild(grid);
        }
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