using CuteSakikoMod.CuteSakikoModCode.NetMessage;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using Godot;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes;

public partial class ChordManagementScreen : Control
{
    private const string LocTable = "rest_site_ui";

    private static ChordManagementScreen? _currentOpenScreen;

    private int _currentTab;
    private Button _equipTabButton;
    private VBoxContainer _leftSlotsContainer;
    private Button _libraryTabButton;
    public bool IsReadOnly { get; private set; }
    private ScrollContainer _rightScroll;
    private VBoxContainer _rightWarehouseContainer;
    private List<string> _tempBonusChords = new();
    private Dictionary<ChordCategory, List<string>> _tempCategorySlots = new();

    public ChordStorageCapability Chords { get; private set; } = null!;

    public void SetChords(ChordStorageCapability chords)
    {
        Chords = chords;
        if (chords != null)
        {
            _tempCategorySlots = new Dictionary<ChordCategory, List<string>>();
            foreach (var cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
            {
                var slots = chords.GetCategorySlots(cat).ToList();
                if (!IsReadOnly)
                    while (slots.Count < chords.GetMaxChordsPerCategory())
                        slots.Add("");
                _tempCategorySlots[cat] = slots;
            }

            _tempBonusChords = new List<string>(chords.GetBonusChords());
        }
    }

    public void SetReadOnly(bool readOnly)
    {
        IsReadOnly = readOnly;
    }

    public void ShowScreen()
    {
        if (_currentOpenScreen != null && IsInstanceValid(_currentOpenScreen)) _currentOpenScreen.QueueFree();
        _currentOpenScreen = this;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        NRun.Instance.GlobalUi.AddChild(this);
    }

    public override void _ExitTree()
    {
        if (_currentOpenScreen == this)
            _currentOpenScreen = null;
        base._ExitTree();
    }

    public override void _Ready()
    {
        var bg = new ColorRect { Color = new Color(0, 0, 0, 0.7f) };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var tabBar = new HBoxContainer();
        tabBar.Alignment = BoxContainer.AlignmentMode.Center;
        tabBar.AddThemeConstantOverride("separation", 0);
        tabBar.AnchorLeft = 0.2f;
        tabBar.AnchorRight = 0.8f;
        tabBar.AnchorTop = 0;
        tabBar.OffsetTop = 80;
        tabBar.OffsetBottom = 120;
        AddChild(tabBar);

        _equipTabButton = CreateTabButton("已记忆和弦");
        _libraryTabButton = CreateTabButton("和弦图鉴");
        _equipTabButton.Pressed += () => SwitchTab(0);
        _libraryTabButton.Pressed += () => SwitchTab(1);
        tabBar.AddChild(_equipTabButton);
        tabBar.AddChild(_libraryTabButton);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 30);
        hbox.AnchorLeft = 0.05f;
        hbox.AnchorRight = 0.95f;
        hbox.AnchorTop = 0.15f;
        hbox.AnchorBottom = 0.85f;
        AddChild(hbox);

        var leftPanel = new Panel { CustomMinimumSize = new Vector2(450, 0) };
        var leftScroll = new ScrollContainer();
        leftPanel.AddChild(leftScroll);
        leftScroll.SetAnchorsPreset(LayoutPreset.FullRect);
        _leftSlotsContainer = new VBoxContainer();
        _leftSlotsContainer.AddThemeConstantOverride("separation", 15);
        leftScroll.AddChild(_leftSlotsContainer);
        hbox.AddChild(leftPanel);

        var rightPanel = new Panel { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _rightScroll = new ScrollContainer();
        rightPanel.AddChild(_rightScroll);
        _rightScroll.SetAnchorsPreset(LayoutPreset.FullRect);
        _rightWarehouseContainer = new VBoxContainer();
        _rightWarehouseContainer.AddThemeConstantOverride("separation", 10);
        _rightScroll.AddChild(_rightWarehouseContainer);
        hbox.AddChild(rightPanel);

        var buttonBar = new HBoxContainer();
        buttonBar.Alignment = BoxContainer.AlignmentMode.Center;
        buttonBar.AddThemeConstantOverride("separation", 20);
        buttonBar.AnchorLeft = 0;
        buttonBar.AnchorRight = 1;
        buttonBar.AnchorTop = 0.88f;
        buttonBar.AnchorBottom = 0.95f;
        AddChild(buttonBar);

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
        StyleBoxFlat hoverStyle = new()
        {
            BgColor = new Color(0.85f, 0.85f, 0.85f),
            CornerRadiusTopLeft = 10,
            CornerRadiusTopRight = 10,
            CornerRadiusBottomLeft = 10,
            CornerRadiusBottomRight = 10,
            ContentMarginLeft = 10,
            ContentMarginRight = 10,
            ContentMarginTop = 5,
            ContentMarginBottom = 5
        };

        if (IsReadOnly)
        {
            var backButton = new Button
            {
                Text = new LocString(LocTable, "CUTE_SAKIKO_MOD_RETURN").GetFormattedText(),
                CustomMinimumSize = new Vector2(120, 40)
            };
            ApplyButtonStyle(backButton, normalStyle, hoverStyle);
            backButton.Pressed += () => QueueFree();
            buttonBar.AddChild(backButton);
        }
        else
        {
            var cancelButton = new Button
            {
                Text = new LocString(LocTable, "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CANCEL").GetFormattedText(),
                CustomMinimumSize = new Vector2(120, 40)
            };
            var confirmButton = new Button
            {
                Text = new LocString(LocTable, "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CONFIRM").GetFormattedText(),
                CustomMinimumSize = new Vector2(120, 40)
            };
            ApplyButtonStyle(cancelButton, normalStyle, hoverStyle);
            ApplyButtonStyle(confirmButton, normalStyle, hoverStyle);
            cancelButton.Pressed += OnCancel;
            confirmButton.Pressed += OnConfirm;
            buttonBar.AddChild(cancelButton);
            buttonBar.AddChild(confirmButton);
        }

        GetViewport().SizeChanged += () => RefreshAll();
        CallDeferred(nameof(RefreshAll));
    }

    private void SwitchTab(int tab)
    {
        _currentTab = tab;
        UpdateTabButtons();
        RefreshAll();
    }

    private void UpdateTabButtons()
    {
        var activeStyle = new StyleBoxFlat
        {
            BgColor = new Color(0.25f, 0.25f, 0.25f),
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5
        };
        var inactiveStyle = new StyleBoxFlat
        {
            BgColor = Colors.White,
            CornerRadiusTopLeft = 5,
            CornerRadiusTopRight = 5,
            CornerRadiusBottomLeft = 5,
            CornerRadiusBottomRight = 5
        };
        _equipTabButton.AddThemeStyleboxOverride("normal", _currentTab == 0 ? activeStyle : inactiveStyle);
        _equipTabButton.AddThemeColorOverride("font_color", _currentTab == 0 ? Colors.White : Colors.Black);
        _libraryTabButton.AddThemeStyleboxOverride("normal", _currentTab == 1 ? activeStyle : inactiveStyle);
        _libraryTabButton.AddThemeColorOverride("font_color", _currentTab == 1 ? Colors.White : Colors.Black);
    }

    private Button CreateTabButton(string text)
        => new() { Text = text, CustomMinimumSize = new Vector2(150, 30) };

    private static void ApplyButtonStyle(Button button, StyleBoxFlat normal, StyleBoxFlat hover)
    {
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", hover);
        button.AddThemeColorOverride("font_color", Colors.Black);
    }

    private void OnConfirm()
    {
        if (Chords == null) return;

        foreach (var cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
        {
            var existing = Chords.GetCategorySlots(cat).ToList();
            foreach (var id in existing)
                Chords.RemoveEquippedChord(cat, id);

            if (_tempCategorySlots.TryGetValue(cat, out var slots))
                foreach (var id in slots)
                    if (!string.IsNullOrEmpty(id))
                        Chords.AddEquippedChord(cat, id);
        }

        var oldBonus = Chords.GetBonusChords().ToList();
        for (var i = oldBonus.Count - 1; i >= 0; i--)
            Chords.RemoveBonusChord(oldBonus[i]);
        foreach (var id in _tempBonusChords)
            Chords.AddBonusChord(id);

        Chords.SyncToSaved();
        SyncIfMultiplayer();
        QueueFree();
    }

    private void OnCancel()
    {
        QueueFree();
    }

    public void SetTempSlot(ChordCategory slotCategory, int index, string newChordId)
    {
        if (IsReadOnly) return;
        if (slotCategory == ChordCategory.Bonus)
        {
            if (index >= 0 && index < _tempBonusChords.Count)
                _tempBonusChords[index] = newChordId;
        }
        else
        {
            if (_tempCategorySlots.TryGetValue(slotCategory, out var slots) && index >= 0 && index < slots.Count)
                slots[index] = newChordId;
        }

        RefreshAll();
    }

    public int GetBonusChordIndex(string chordId) => _tempBonusChords.IndexOf(chordId);

    public void SyncIfMultiplayer()
    {
        if (Chords == null) return;
        var player = Chords.HostPlayer;
        if (player == null) return;

        var netService = RunManager.Instance.NetService;
        if (netService != null && netService.Type != NetGameType.Singleplayer)
        {
            var chordsData = string.Join(";",
                Chords.GetCategorySlots(ChordCategory.Major).Select(id => $"{(int)ChordCategory.Major}:{id}")
                    .Concat(Chords.GetCategorySlots(ChordCategory.Minor)
                        .Select(id => $"{(int)ChordCategory.Minor}:{id}"))
                    .Concat(Chords.GetCategorySlots(ChordCategory.Dominant)
                        .Select(id => $"{(int)ChordCategory.Dominant}:{id}")));

            var msg = new ChordSyncMessage
            {
                PlayerNetId = player.NetId,
                ChordsData = chordsData,
                BonusChordsData = string.Join(";", Chords.GetBonusChords()),
                LearnedChordsData = string.Join(";", Chords.GetLearnedChords())
            };
            netService.SendMessage(msg);
        }
    }

    public void RefreshAll()
    {
        RefreshLeftSlots();
        RefreshRightWarehouse();
    }

    public void RefreshLeftSlots()
    {
        foreach (var child in _leftSlotsContainer.GetChildren())
            child.QueueFree();

        if (Chords == null) return;

        if (_currentTab == 1)
        {
            (_leftSlotsContainer.GetParent() as Control).Visible = false;
            return;
        }

        (_leftSlotsContainer.GetParent() as Control).Visible = true;

        foreach (var cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
        {
            var slots = IsReadOnly
                ? Chords.GetCategorySlots(cat).Select(id => string.IsNullOrEmpty(id) ? "" : id).ToList()
                : _tempCategorySlots.GetValueOrDefault(cat, new List<string>());

            if (IsReadOnly)
                for (var i = 0; i < slots.Count; i++)
                {
                    var label = $"{new LocString(LocTable, GetSlotLocKey(cat)).GetFormattedText()} {i + 1}";
                    AddStaticSlotRow(label, slots[i]);
                }
            else
                for (var i = 0; i < slots.Count; i++)
                {
                    var label = $"{new LocString(LocTable, GetSlotLocKey(cat)).GetFormattedText()} {i + 1}";
                    AddSlotRow(label, cat, i, slots[i]);
                }
        }

        var bonusChords = IsReadOnly ? Chords.GetBonusChords() : _tempBonusChords;
        if (IsReadOnly)
            for (var i = 0; i < bonusChords.Count; i++)
            {
                var baseText = new LocString(LocTable, "CUTE_SAKIKO_MOD_SLOT_BONUS").GetFormattedText();
                AddStaticSlotRow($"{baseText} {i + 1}", bonusChords[i]);
            }
        else
            for (var i = 0; i < bonusChords.Count; i++)
            {
                var baseText = new LocString(LocTable, "CUTE_SAKIKO_MOD_SLOT_BONUS").GetFormattedText();
                AddSlotRow($"{baseText} {i + 1}", ChordCategory.Bonus, i, bonusChords[i]);
            }
    }

    private static string GetSlotLocKey(ChordCategory cat)
    {
        return cat switch
        {
            ChordCategory.Major => "CUTE_SAKIKO_MOD_SLOT_MAJOR",
            ChordCategory.Minor => "CUTE_SAKIKO_MOD_SLOT_MINOR",
            ChordCategory.Dominant => "CUTE_SAKIKO_MOD_SLOT_DOMINANT",
            _ => "CUTE_SAKIKO_MOD_SLOT_BONUS"
        };
    }

    private void AddSlotRow(string labelText, ChordCategory slotCategory, int slotIndex, string currentChordId)
    {
        var hbox = new HBoxContainer();
        hbox.AddChild(new Label { Text = labelText, CustomMinimumSize = new Vector2(130, 40) });
        var dropArea = new ChordSlotDropTarget(slotCategory, slotIndex, currentChordId, this);
        hbox.AddChild(dropArea);
        _leftSlotsContainer.AddChild(hbox);
    }

    private void AddStaticSlotRow(string labelText, string chordId)
    {
        var hbox = new HBoxContainer();
        hbox.AddChild(new Label { Text = labelText, CustomMinimumSize = new Vector2(130, 40) });
        var iconControl = new Control { CustomMinimumSize = new Vector2(80, 80) };
        if (!string.IsNullOrEmpty(chordId))
        {
            var texture = ChordDisplayHelper.GetChordTexture(chordId);
            if (texture != null)
            {
                var img = new TextureRect
                {
                    Texture = texture,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    MouseFilter = MouseFilterEnum.Ignore
                };
                img.SetAnchorsPreset(LayoutPreset.FullRect);
                iconControl.AddChild(img);
            }

            iconControl.MouseEntered += () =>
            {
                var player = Chords?.HostPlayer;
                if (player?.Creature != null && Chords != null)
                {
                    var tip = ChordDisplayHelper.GetDynamicChordHoverTip(chordId, player.Creature,
                        Chords.GetDisplayBonus());
                    ShowHoverTip(iconControl, tip);
                }
            };
            iconControl.MouseExited += () => NHoverTipSet.Remove(iconControl);
        }

        hbox.AddChild(iconControl);
        _leftSlotsContainer.AddChild(hbox);
    }

    public void RefreshRightWarehouse()
    {
        if (_rightScroll == null) return;
        if (_rightScroll.Size.X <= 0)
        {
            CallDeferred(nameof(RefreshRightWarehouse));
            return;
        }

        foreach (var child in _rightWarehouseContainer.GetChildren())
            child.QueueFree();

        if (Chords == null) return;

        if (_currentTab == 0)
        {
            var learned = Chords.GetLearnedChords();
            var grouped = learned
                .Select(id => ChordManager.AllChords.TryGetValue(id, out var def) ? def : null)
                .Where(c => c != null)
                .GroupBy(c => c.Category)
                .OrderBy(g => g.Key switch
                {
                    ChordCategory.Major => 0, ChordCategory.Minor => 1, ChordCategory.Dominant => 2, _ => 3
                });

            foreach (var group in grouped)
            {
                var catLabel = new Label { Text = GetCategoryDisplayText(group.Key) };
                catLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
                catLabel.AddThemeFontSizeOverride("font_size", 16);
                _rightWarehouseContainer.AddChild(catLabel);

                var grid = new GridContainer { Columns = CalculateColumns() };
                foreach (var chord in group)
                    if (IsReadOnly)
                    {
                        var iconControl = new Control { CustomMinimumSize = new Vector2(80, 80) };
                        var texture = ChordDisplayHelper.GetChordTexture(chord.Id);
                        if (texture != null)
                        {
                            var img = new TextureRect
                            {
                                Texture = texture,
                                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                                MouseFilter = MouseFilterEnum.Ignore
                            };
                            img.SetAnchorsPreset(LayoutPreset.FullRect);
                            iconControl.AddChild(img);
                        }

                        iconControl.MouseEntered += () =>
                        {
                            var player = Chords?.HostPlayer;
                            if (player?.Creature != null && Chords != null)
                            {
                                var tip = ChordDisplayHelper.GetDynamicChordHoverTip(chord.Id, player.Creature,
                                    Chords.GetDisplayBonus());
                                ShowHoverTip(iconControl, tip);
                            }
                        };
                        iconControl.MouseExited += () => NHoverTipSet.Remove(iconControl);
                        grid.AddChild(iconControl);
                    }
                    else
                    {
                        var dragSource = new ChordDragSource(chord.Id, this);
                        grid.AddChild(dragSource);
                    }

                _rightWarehouseContainer.AddChild(grid);
            }
        }
        else
        {
            var allChords = ChordManager.AllChordsList;
            var grouped = allChords
                .Where(c => !c.IsTemporaryOnly)
                .GroupBy(c => c.Category)
                .OrderBy(g => g.Key switch
                {
                    ChordCategory.Major => 0, ChordCategory.Minor => 1, ChordCategory.Dominant => 2, _ => 3
                });

            foreach (var group in grouped)
            {
                var catLabel = new Label { Text = GetCategoryDisplayText(group.Key) };
                catLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
                catLabel.AddThemeFontSizeOverride("font_size", 16);
                _rightWarehouseContainer.AddChild(catLabel);

                var grid = new GridContainer { Columns = CalculateColumns() };
                foreach (var chord in group)
                {
                    var iconControl = new Control { CustomMinimumSize = new Vector2(80, 80) };
                    var texture = ChordDisplayHelper.GetChordTexture(chord.Id);
                    if (texture != null)
                    {
                        var img = new TextureRect
                        {
                            Texture = texture,
                            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                            MouseFilter = MouseFilterEnum.Ignore
                        };
                        img.SetAnchorsPreset(LayoutPreset.FullRect);
                        iconControl.AddChild(img);
                    }

                    iconControl.MouseEntered += () =>
                    {
                        var player = Chords?.HostPlayer;
                        if (player?.Creature != null && Chords != null)
                        {
                            var tip = ChordDisplayHelper.GetDynamicChordHoverTip(chord.Id, player.Creature,
                                Chords.GetDisplayBonus());
                            ShowHoverTip(iconControl, tip);
                        }
                    };
                    iconControl.MouseExited += () => NHoverTipSet.Remove(iconControl);
                    grid.AddChild(iconControl);
                }

                _rightWarehouseContainer.AddChild(grid);
            }
        }
    }

    private int CalculateColumns()
    {
        if (_rightScroll == null) return 4;
        var available = _rightScroll.Size.X - 20;
        if (available <= 0) available = 400;
        return Mathf.Max(1, Mathf.FloorToInt(available / 90));
    }

    private static void ShowHoverTip(Control owner, HoverTip tip)
    {
        var tipSet = NHoverTipSet.CreateAndShow(owner, tip);
        if (tipSet == null) return;
        var alignment = owner.GlobalPosition.X > owner.GetViewportRect().Size.X * 0.5f
            ? HoverTipAlignment.Left
            : HoverTipAlignment.Right;
        tipSet.SetAlignment(owner, alignment);
        var pos = tipSet.GlobalPosition;
        var viewportRect = owner.GetViewportRect();
        if (pos.X < 10) pos.X = 10;
        if (pos.X + tipSet.Size.X > viewportRect.Size.X - 10) pos.X = viewportRect.Size.X - tipSet.Size.X - 10;
        if (pos.Y < 10) pos.Y = 10;
        if (pos.Y + tipSet.Size.Y > viewportRect.Size.Y - 10) pos.Y = viewportRect.Size.Y - tipSet.Size.Y - 10;
        tipSet.GlobalPosition = pos;
    }

    public static string GetCategoryDisplayText(ChordCategory cat)
    {
        return cat switch
        {
            ChordCategory.Major => new LocString(LocTable, "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CATEGORY_MAJOR").GetFormattedText(),
            ChordCategory.Minor => new LocString(LocTable, "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CATEGORY_MINOR").GetFormattedText(),
            ChordCategory.Dominant => new LocString(LocTable, "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CATEGORY_DOMINANT").GetFormattedText(),
            ChordCategory.Anon => new LocString(LocTable, "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CATEGORY_ANON").GetFormattedText(),
            _ => new LocString(LocTable, "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CATEGORY_OTHER").GetFormattedText()
        };
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
        {
            OnCancel();
            AcceptEvent();
        }
    }
}