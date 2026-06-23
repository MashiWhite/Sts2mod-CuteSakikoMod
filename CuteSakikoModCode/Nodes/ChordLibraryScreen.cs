using CuteSakikoMod.CuteSakikoModCode.Systems;
using Godot;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    private string _titleTable;
    private string _titleKey;
    private LocString? _freePromptLoc;
    private List<string>? _candidateIds;

    private Label _titleLabel = null!;
    private Button _confirmButton = null!;
    private Button _cancelButton = null!;

    private const string LocTable = "rest_site_ui";

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
        return await ShowSelectionInternal(
            count,
            LocTable,
            "CUTE_SAKIKO_MOD_CHORD_LIBRARY_SELECT_TITLE",
            null
        );
    }

    public async Task<List<string>> ShowFreeSelection(List<string> candidateIds, LocString prompt)
    {
        _candidateIds = candidateIds;
        return await ShowSelectionInternal(
            int.MaxValue,
            LocTable,
            "CUTE_SAKIKO_MOD_CHORD_LIBRARY_FREE_SELECT_TITLE",
            prompt
        );
    }

    private async Task<List<string>> ShowSelectionInternal(
        int count, string table, string key, LocString? prompt)
    {
        if (_selectionTcs != null && !_selectionTcs.Task.IsCompleted)
            _selectionTcs.TrySetCanceled();

        _selectionTcs = new TaskCompletionSource<List<string>>();
        _selectedChords = new List<string>();
        _targetCount = count;
        _isSelectMode = true;
        _isCancelled = false;
        _titleTable = table;
        _titleKey = key;
        _freePromptLoc = prompt;

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
        var topMargin = 80f;
        bg.OffsetTop = topMargin;
        bg.OffsetBottom = 0;
        bg.OffsetLeft = 0;
        bg.OffsetRight = 0;
        if (!_isSelectMode)
            bg.GuiInput += e =>
            {
                if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && mb.Pressed)
                    QueueFree();
            };
        AddChild(bg);

        // 顶部栏
        var topBar = new ColorRect
        {
            Color = new Color(0.15f, 0.1f, 0.2f),
            MouseFilter = MouseFilterEnum.Ignore
        };
        topBar.AnchorLeft = 0;
        topBar.AnchorRight = 1;
        topBar.AnchorTop = 0;
        topBar.AnchorBottom = 0;
        topBar.OffsetTop = topMargin;
        topBar.OffsetBottom = topMargin + 60f;
        AddChild(topBar);

        _titleLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _titleLabel.AnchorLeft = 0;
        _titleLabel.AnchorRight = 1;
        _titleLabel.AnchorTop = 0;
        _titleLabel.AnchorBottom = 1;
        _titleLabel.AddThemeFontSizeOverride("font_size", 24);
        _titleLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
        topBar.AddChild(_titleLabel);

        // 滚动区域
        var scroll = new ScrollContainer();
        scroll.AnchorLeft = 0;
        scroll.AnchorRight = 1;
        scroll.AnchorTop = 0;
        scroll.AnchorBottom = 1;
        var leftMargin = Mathf.Max(200f, GetViewportRect().Size.X * 0.15f);
        var rightMargin = -Mathf.Max(40f, GetViewportRect().Size.X * 0.05f);
        scroll.OffsetLeft = leftMargin;
        scroll.OffsetRight = rightMargin;
        scroll.OffsetTop = topBar.OffsetBottom + 20f;
        scroll.OffsetBottom = bg.OffsetBottom + 20f;
        AddChild(scroll);

        // 内容容器
        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 24);
        scroll.AddChild(vbox);

        // 根据模式确定显示的和弦
        Dictionary<ChordCategory, List<ChordDefinition>> chordsByCategory;
        if (_isSelectMode && _candidateIds != null)
        {
            chordsByCategory = new();
            foreach (var id in _candidateIds)
            {
                if (ChordManager.AllChords.TryGetValue(id, out var def))
                {
                    if (!chordsByCategory.ContainsKey(def.Category))
                        chordsByCategory[def.Category] = new();
                    chordsByCategory[def.Category].Add(def);
                }
            }
        }
        else
        {
            chordsByCategory = new()
            {
                { ChordCategory.Major, ChordManager.AllChordsList.Where(c => c.Category == ChordCategory.Major).ToList() },
                { ChordCategory.Minor, ChordManager.AllChordsList.Where(c => c.Category == ChordCategory.Minor).ToList() },
                { ChordCategory.Dominant, ChordManager.AllChordsList.Where(c => c.Category == ChordCategory.Dominant).ToList() },
                { ChordCategory.Anon, ChordManager.AllChordsList.Where(c => c.Category == ChordCategory.Anon).ToList() },
                { ChordCategory.Bonus, ChordManager.AllChordsList.Where(c => c.Category == ChordCategory.Bonus).ToList() }
            };
        }

        foreach (var kv in chordsByCategory)
        {
            if (kv.Value.Count == 0) continue;

            var catLoc = GetCategoryLocString(kv.Key);
            var catLabel = new Label { Text = catLoc.GetFormattedText() };
            catLabel.AddThemeFontSizeOverride("font_size", 20);
            catLabel.AddThemeColorOverride("font_color", new Color(1f, 0.85f, 0.2f));
            vbox.AddChild(catLabel);

            var grid = new GridContainer();
            float availableWidth = scroll.Size.X - 40f;
            int columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / 120f));
            grid.Columns = columns;
            foreach (var chordDef in kv.Value)
            {
                var btn = new ChordButton();
                btn.Setup(chordDef.Id);
                btn.Modulate = _isSelectMode && _selectedChords.Contains(chordDef.Id)
                    ? new Color(1, 1, 0.5f) : Colors.White;

                if (_isSelectMode)
                {
                    var chordId = chordDef.Id;
                    btn.Pressed += () => OnChordButtonPressed(chordId);
                }
                _chordButtons[chordDef.Id] = btn;
                grid.AddChild(btn);
            }
            vbox.AddChild(grid);
        }

        // 自由选择模式下的确认/取消按钮
        if (_isSelectMode && _targetCount == int.MaxValue)
        {
            var buttonBar = new HBoxContainer();
            buttonBar.Alignment = BoxContainer.AlignmentMode.Center;
            buttonBar.AddThemeConstantOverride("separation", 20);

            // 正常状态：白底圆角
            StyleBoxFlat normalStyle = new()
            {
                BgColor = Colors.White,
                CornerRadiusTopLeft = 10,
                CornerRadiusTopRight = 10,
                CornerRadiusBottomLeft = 10,
                CornerRadiusBottomRight = 10,
                ContentMarginLeft = 10,
                ContentMarginRight = 10,
                ContentMarginTop = 5,
                ContentMarginBottom = 5
            };

            // 悬浮/按下状态：灰色背景，圆角保持不变
            StyleBoxFlat hoverStyle = new()
            {
                BgColor = new Color(0.75f, 0.75f, 0.75f), // 浅灰色
                CornerRadiusTopLeft = 10,
                CornerRadiusTopRight = 10,
                CornerRadiusBottomLeft = 10,
                CornerRadiusBottomRight = 10,
                ContentMarginLeft = 10,
                ContentMarginRight = 10,
                ContentMarginTop = 5,
                ContentMarginBottom = 5
            };

            _confirmButton = new Button
            {
                Text = new LocString(LocTable, "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CONFIRM").GetFormattedText()
            };
            _confirmButton.AddThemeStyleboxOverride("normal", normalStyle);
            _confirmButton.AddThemeStyleboxOverride("hover", hoverStyle);
            _confirmButton.AddThemeStyleboxOverride("pressed", hoverStyle);
            _confirmButton.AddThemeColorOverride("font_color", Colors.Black);
            _confirmButton.Pressed += () =>
            {
                _selectionTcs?.TrySetResult(_selectedChords.ToList());
                QueueFree();
            };

            _cancelButton = new Button
            {
                Text = new LocString(LocTable, "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CANCEL").GetFormattedText()
            };
            _cancelButton.AddThemeStyleboxOverride("normal", normalStyle);
            _cancelButton.AddThemeStyleboxOverride("hover", hoverStyle);
            _cancelButton.AddThemeStyleboxOverride("pressed", hoverStyle);
            _cancelButton.AddThemeColorOverride("font_color", Colors.Black);
            _cancelButton.Pressed += () =>
            {
                _isCancelled = true;
                _selectionTcs?.TrySetResult(new List<string>());
                QueueFree();
            };

            buttonBar.AddChild(_confirmButton);
            buttonBar.AddChild(_cancelButton);
            vbox.AddChild(buttonBar);
        }

        UpdateTitleLabel();
        GetViewport().SizeChanged += OnViewportSizeChanged;
    }

    private void OnViewportSizeChanged() { }

    private void OnChordButtonPressed(string chordId)
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
            if (_targetCount != int.MaxValue && _selectedChords.Count >= _targetCount) return;
            _selectedChords.Add(chordId);
            if (_chordButtons.TryGetValue(chordId, out var btn))
                btn.Modulate = new Color(1, 1, 0.5f);
        }

        UpdateTitleLabel();

        if (_targetCount != int.MaxValue && _selectedChords.Count == _targetCount)
        {
            _selectionTcs?.TrySetResult(_selectedChords.ToList());
            QueueFree();
        }
    }

    private void UpdateTitleLabel()
    {
        if (_titleLabel == null) return;

        if (!_isSelectMode)
        {
            _titleLabel.Text = new LocString(
                LocTable, "CUTE_SAKIKO_MOD_CHORD_LIBRARY_BROWSE_TITLE"
            ).GetFormattedText();
            return;
        }

        var loc = new LocString(_titleTable, _titleKey);
        if (_targetCount != int.MaxValue)
        {
            loc.Add("Count", (decimal)_targetCount);
        }
        if (_freePromptLoc != null)
        {
            loc.Add("Prompt", _freePromptLoc.GetFormattedText());
        }
        loc.Add("Selected", (decimal)_selectedChords.Count);
        _titleLabel.Text = loc.GetFormattedText();
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

    private static LocString GetCategoryLocString(ChordCategory cat)
    {
        string key = cat switch
        {
            ChordCategory.Major => "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CATEGORY_MAJOR",
            ChordCategory.Minor => "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CATEGORY_MINOR",
            ChordCategory.Dominant => "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CATEGORY_DOMINANT",
            ChordCategory.Anon => "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CATEGORY_ANON",
            ChordCategory.Bonus => "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CATEGORY_BONUS",
            _ => "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CATEGORY_OTHER"
        };
        return new LocString(LocTable, key);
    }
}