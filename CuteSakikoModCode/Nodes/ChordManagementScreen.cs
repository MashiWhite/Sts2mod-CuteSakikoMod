using System.Collections.Generic;
using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.NetMessage;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using Godot;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes;

public partial class ChordManagementScreen : Control
{
    public AnonGuitar Guitar { get; private set; }
    private VBoxContainer _leftSlotsContainer;
    private VBoxContainer _rightWarehouseContainer;
    public bool _readOnly;

    // 临时状态：每个类别一个列表，Bonus 单独列表
    private Dictionary<ChordCategory, List<string>> _tempCategorySlots = new();
    private List<string> _tempBonusChords = new();

    private const string LocTable = "rest_site_ui";

    // 当前显示的标签页：0 = 装备，1 = 和弦图鉴
    private int _currentTab = 0;
    private Button _equipTabButton;
    private Button _libraryTabButton;
    private ScrollContainer _rightScroll; // 用于获取宽度计算列数

    public void SetGuitar(AnonGuitar guitar)
    {
        Guitar = guitar;
        if (guitar != null)
        {
            _tempCategorySlots = new Dictionary<ChordCategory, List<string>>();
            foreach (var cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
            {
                var slots = guitar.GetCategorySlots(cat).ToList();
                if (!_readOnly)
                {
                    while (slots.Count < guitar.GetMaxChordsPerCategory())
                        slots.Add("");
                }
                _tempCategorySlots[cat] = slots;
            }
            _tempBonusChords = new List<string>(guitar.GetBonusChords());
        }
    }

    public void SetReadOnly(bool readOnly) => _readOnly = readOnly;

    public void ShowScreen()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        NRun.Instance.GlobalUi.AddChild(this);
    }

    public override void _Ready()
    {
        // 背景
        var bg = new ColorRect { Color = new Color(0, 0, 0, 0.7f) };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        // 顶部标签栏
        var tabBar = new HBoxContainer();
        tabBar.Alignment = BoxContainer.AlignmentMode.Center;
        tabBar.AddThemeConstantOverride("separation", 0);
        tabBar.AnchorLeft = 0.2f;
        tabBar.AnchorRight = 0.8f;
        tabBar.AnchorTop = 0;
        tabBar.OffsetTop = 80;
        tabBar.OffsetBottom = 120;
        AddChild(tabBar);

        _equipTabButton = CreateTabButton("已记忆和弦", true);
        _libraryTabButton = CreateTabButton("和弦图鉴", false);
        _equipTabButton.Pressed += () => SwitchTab(0);
        _libraryTabButton.Pressed += () => SwitchTab(1);
        tabBar.AddChild(_equipTabButton);
        tabBar.AddChild(_libraryTabButton);

        // 主水平容器（占据剩余空间）
        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 30);
        hbox.AnchorLeft = 0.05f;
        hbox.AnchorRight = 0.95f;
        hbox.AnchorTop = 0.15f;
        hbox.AnchorBottom = 0.85f;
        AddChild(hbox);

        // 左侧面板（槽位）—— 仅在装备标签页可见
        var leftPanel = new Panel { CustomMinimumSize = new Vector2(450, 0) };
        var leftScroll = new ScrollContainer();
        leftPanel.AddChild(leftScroll);
        leftScroll.SetAnchorsPreset(LayoutPreset.FullRect);
        _leftSlotsContainer = new VBoxContainer();
        _leftSlotsContainer.AddThemeConstantOverride("separation", 15);
        leftScroll.AddChild(_leftSlotsContainer);
        hbox.AddChild(leftPanel);

        // 右侧面板（仓库/图鉴）
        var rightPanel = new Panel { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _rightScroll = new ScrollContainer();
        rightPanel.AddChild(_rightScroll);
        _rightScroll.SetAnchorsPreset(LayoutPreset.FullRect);
        _rightWarehouseContainer = new VBoxContainer();
        _rightWarehouseContainer.AddThemeConstantOverride("separation", 10);
        _rightScroll.AddChild(_rightWarehouseContainer);
        hbox.AddChild(rightPanel);

        // 底部按钮（只读模式：返回；编辑模式：取消+确认）
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

        if (_readOnly)
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

        RefreshAll();
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

    private Button CreateTabButton(string text, bool active)
    {
        var btn = new Button { Text = text, CustomMinimumSize = new Vector2(150, 30) };
        return btn;
    }

    private static void ApplyButtonStyle(Button button, StyleBoxFlat normal, StyleBoxFlat hover)
    {
        button.AddThemeStyleboxOverride("normal", normal);
        button.AddThemeStyleboxOverride("hover", hover);
        button.AddThemeStyleboxOverride("pressed", hover);
        button.AddThemeColorOverride("font_color", Colors.Black);
    }

    private void OnConfirm()
    {
        if (Guitar == null) return;

        foreach (var cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
        {
            var existing = Guitar.GetCategorySlots(cat).ToList();
            foreach (var id in existing)
                Guitar.RemoveEquippedChord(cat, id);

            if (_tempCategorySlots.TryGetValue(cat, out var slots))
            {
                foreach (var id in slots)
                {
                    if (!string.IsNullOrEmpty(id))
                        Guitar.AddEquippedChord(cat, id);
                }
            }
        }

        var oldBonus = Guitar.GetBonusChords().ToList();
        for (int i = oldBonus.Count - 1; i >= 0; i--)
            Guitar.RemoveBonusChord(oldBonus[i]);
        foreach (var id in _tempBonusChords)
            Guitar.AddBonusChord(id);

        Guitar.SyncToSaved();
        SyncIfMultiplayer();
        QueueFree();
    }

    private void OnCancel() => QueueFree();

    public void SetTempSlot(ChordCategory slotCategory, int index, string newChordId)
    {
        if (_readOnly) return;
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
        if (Guitar == null) return;
        var netService = RunManager.Instance.NetService;
        if (netService != null && netService.Type != NetGameType.Singleplayer)
        {
            var chordsData = string.Join(";",
                Guitar.GetCategorySlots(ChordCategory.Major).Select(id => $"{(int)ChordCategory.Major}:{id}")
                .Concat(Guitar.GetCategorySlots(ChordCategory.Minor).Select(id => $"{(int)ChordCategory.Minor}:{id}"))
                .Concat(Guitar.GetCategorySlots(ChordCategory.Dominant).Select(id => $"{(int)ChordCategory.Dominant}:{id}")));

            var msg = new ChordSyncMessage
            {
                PlayerNetId = Guitar.Owner.NetId,
                ChordsData = chordsData,
                BonusChordsData = string.Join(";", Guitar.GetBonusChords()),
                LearnedChordsData = string.Join(";", Guitar.GetLearnedChords())
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
        foreach (Node child in _leftSlotsContainer.GetChildren())
            child.QueueFree();

        if (Guitar == null) return;

        if (_currentTab == 1) // 图鉴模式：隐藏左侧槽位
        {
            (_leftSlotsContainer.GetParent() as Control).Visible = false;
            return;
        }
        (_leftSlotsContainer.GetParent() as Control).Visible = true;

        // 主类别槽位
        foreach (var cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
        {
            var slots = _readOnly
                ? Guitar.GetCategorySlots(cat).Select(id => string.IsNullOrEmpty(id) ? "" : id).ToList()
                : _tempCategorySlots.GetValueOrDefault(cat, new List<string>());

            if (_readOnly)
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    var label = $"{new LocString(LocTable, GetSlotLocKey(cat)).GetFormattedText()} {i + 1}";
                    AddStaticSlotRow(label, slots[i]);
                }
            }
            else
            {
                for (int i = 0; i < slots.Count; i++)
                {
                    var label = $"{new LocString(LocTable, GetSlotLocKey(cat)).GetFormattedText()} {i + 1}";
                    AddSlotRow(label, cat, i, slots[i]);
                }
            }
        }

        // Bonus 槽位
        var bonusChords = _readOnly ? Guitar.GetBonusChords() : _tempBonusChords;
        if (_readOnly)
        {
            for (int i = 0; i < bonusChords.Count; i++)
            {
                var baseText = new LocString(LocTable, "CUTE_SAKIKO_MOD_SLOT_BONUS").GetFormattedText();
                AddStaticSlotRow($"{baseText} {i + 1}", bonusChords[i]);
            }
        }
        else
        {
            for (int i = 0; i < bonusChords.Count; i++)
            {
                var baseText = new LocString(LocTable, "CUTE_SAKIKO_MOD_SLOT_BONUS").GetFormattedText();
                AddSlotRow($"{baseText} {i + 1}", ChordCategory.Bonus, i, bonusChords[i]);
            }
        }
    }

    private static string GetSlotLocKey(ChordCategory cat) => cat switch
    {
        ChordCategory.Major => "CUTE_SAKIKO_MOD_SLOT_MAJOR",
        ChordCategory.Minor => "CUTE_SAKIKO_MOD_SLOT_MINOR",
        ChordCategory.Dominant => "CUTE_SAKIKO_MOD_SLOT_DOMINANT",
        _ => "CUTE_SAKIKO_MOD_SLOT_BONUS"
    };

    private void AddSlotRow(string labelText, ChordCategory slotCategory, int slotIndex, string currentChordId)
    {
        var hbox = new HBoxContainer();
        var label = new Label { Text = labelText, CustomMinimumSize = new Vector2(130, 40) };
        hbox.AddChild(label);
        var dropArea = new ChordSlotDropTarget(slotCategory, slotIndex, currentChordId, this);
        hbox.AddChild(dropArea);
        _leftSlotsContainer.AddChild(hbox);
    }

    private void AddStaticSlotRow(string labelText, string chordId)
    {
        var hbox = new HBoxContainer();
        var label = new Label { Text = labelText, CustomMinimumSize = new Vector2(130, 40) };
        hbox.AddChild(label);
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
                    MouseFilter = Control.MouseFilterEnum.Ignore
                };
                img.SetAnchorsPreset(LayoutPreset.FullRect);
                iconControl.AddChild(img);
            }
            iconControl.MouseEntered += () =>
            {
                if (Guitar?.Owner?.Creature != null)
                {
                    var tip = ChordDisplayHelper.GetDynamicChordHoverTip(chordId, Guitar.Owner.Creature, Guitar.GetTotalBonus());
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
        foreach (Node child in _rightWarehouseContainer.GetChildren())
            child.QueueFree();

        if (Guitar == null) return;

        if (_currentTab == 0) // 装备标签页：显示已学习和弦仓库
        {
            var learned = Guitar.GetLearnedChords();
            var grouped = learned
                .Select(id => ChordManager.AllChords.TryGetValue(id, out var def) ? def : null)
                .Where(c => c != null)
                .GroupBy(c => c.Category)
                .OrderBy(g => g.Key switch { ChordCategory.Major => 0, ChordCategory.Minor => 1, ChordCategory.Dominant => 2, _ => 3 });

            foreach (var group in grouped)
            {
                var catText = GetCategoryDisplayText(group.Key);
                var catLabel = new Label { Text = catText };
                catLabel.AddThemeColorOverride("font_color", new Color(0.8f, 0.8f, 0.8f));
                catLabel.AddThemeFontSizeOverride("font_size", 16);
                _rightWarehouseContainer.AddChild(catLabel);

                var grid = new GridContainer { Columns = CalculateColumns() };
                foreach (var chord in group)
                {
                    if (_readOnly)
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
                                MouseFilter = Control.MouseFilterEnum.Ignore
                            };
                            img.SetAnchorsPreset(LayoutPreset.FullRect);
                            iconControl.AddChild(img);
                        }
                        iconControl.MouseEntered += () =>
                        {
                            if (Guitar?.Owner?.Creature != null)
                            {
                                var tip = ChordDisplayHelper.GetDynamicChordHoverTip(chord.Id, Guitar.Owner.Creature, Guitar.GetTotalBonus());
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
                }
                _rightWarehouseContainer.AddChild(grid);
            }
        }
        else // 图鉴标签页：显示所有和弦（只读浏览）
        {
            var allChords = ChordManager.AllChordsList;
            var grouped = allChords
                .Where(c => !c.IsTemporaryOnly)
                .GroupBy(c => c.Category)
                .OrderBy(g => g.Key switch { ChordCategory.Major => 0, ChordCategory.Minor => 1, ChordCategory.Dominant => 2, _ => 3 });

            foreach (var group in grouped)
            {
                var catText = GetCategoryDisplayText(group.Key);
                var catLabel = new Label { Text = catText };
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
                            MouseFilter = Control.MouseFilterEnum.Ignore
                        };
                        img.SetAnchorsPreset(LayoutPreset.FullRect);
                        iconControl.AddChild(img);
                    }
                    iconControl.MouseEntered += () =>
                    {
                        if (Guitar?.Owner?.Creature != null)
                        {
                            var tip = ChordDisplayHelper.GetDynamicChordHoverTip(chord.Id, Guitar.Owner.Creature, Guitar.GetTotalBonus());
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

    /// <summary>
    /// 根据右侧容器可用宽度动态计算每行列数。每个和弦图标约占 90 像素宽。
    /// </summary>
    private int CalculateColumns()
    {
        if (_rightScroll == null) return 4;
        float available = _rightScroll.Size.X - 20; // 减去滚动条宽度
        if (available <= 0) available = 400; // 兜底
        return Mathf.Max(1, Mathf.FloorToInt(available / 90));
    }

    private static void ShowHoverTip(Control owner, HoverTip tip)
    {
        var tipSet = NHoverTipSet.CreateAndShow(owner, tip);
        if (tipSet == null) return;
        var alignment = owner.GlobalPosition.X > owner.GetViewportRect().Size.X * 0.5f ? HoverTipAlignment.Left : HoverTipAlignment.Right;
        tipSet.SetAlignment(owner, alignment);
        var pos = tipSet.GlobalPosition;
        var viewportRect = owner.GetViewportRect();
        if (pos.X < 10) pos.X = 10;
        if (pos.X + tipSet.Size.X > viewportRect.Size.X - 10) pos.X = viewportRect.Size.X - tipSet.Size.X - 10;
        if (pos.Y < 10) pos.Y = 10;
        if (pos.Y + tipSet.Size.Y > viewportRect.Size.Y - 10) pos.Y = viewportRect.Size.Y - tipSet.Size.Y - 10;
        tipSet.GlobalPosition = pos;
    }

    public static string GetCategoryDisplayText(ChordCategory cat) => cat switch
    {
        ChordCategory.Major => new LocString(LocTable, "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CATEGORY_MAJOR").GetFormattedText(),
        ChordCategory.Minor => new LocString(LocTable, "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CATEGORY_MINOR").GetFormattedText(),
        ChordCategory.Dominant => new LocString(LocTable, "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CATEGORY_DOMINANT").GetFormattedText(),
        ChordCategory.Anon => new LocString(LocTable, "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CATEGORY_ANON").GetFormattedText(),
        _ => new LocString(LocTable, "CUTE_SAKIKO_MOD_CHORD_LIBRARY_CATEGORY_OTHER").GetFormattedText()
    };

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
        {
            OnCancel();
            AcceptEvent();
        }
    }
}