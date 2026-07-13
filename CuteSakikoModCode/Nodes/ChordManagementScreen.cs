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

    // 临时状态（确认前不影响遗物）
    private Dictionary<ChordCategory, string> _tempCurrentChords = new();
    private List<string> _tempBonusChords = new();

    private const string LocTable = "rest_site_ui";

    public void SetGuitar(AnonGuitar guitar)
    {
        Guitar = guitar;
        if (guitar != null)
        {
            _tempCurrentChords = new Dictionary<ChordCategory, string>(guitar.GetCurrentChords());
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
        var bg = new ColorRect { Color = new Color(0, 0, 0, 0.7f) };
        bg.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(bg);

        var titleLabel = new Label
        {
            Text = new LocString(LocTable, "CUTE_SAKIKO_MOD_OPTION_PRACTICE_GUITAR_NAME").GetFormattedText(),
            HorizontalAlignment = HorizontalAlignment.Center
        };
        titleLabel.AddThemeColorOverride("font_color", new Color(1, 0.85f, 0.2f));
        titleLabel.AddThemeFontSizeOverride("font_size", 28);
        titleLabel.AnchorLeft = 0;
        titleLabel.AnchorRight = 1;
        titleLabel.AnchorTop = 0;
        titleLabel.OffsetTop = 60;
        titleLabel.OffsetBottom = 100;
        AddChild(titleLabel);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 30);
        hbox.AnchorLeft = 0.05f;
        hbox.AnchorRight = 0.95f;
        hbox.AnchorTop = 0.12f;
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
        var rightScroll = new ScrollContainer();
        rightPanel.AddChild(rightScroll);
        rightScroll.SetAnchorsPreset(LayoutPreset.FullRect);
        _rightWarehouseContainer = new VBoxContainer();
        _rightWarehouseContainer.AddThemeConstantOverride("separation", 10);
        rightScroll.AddChild(_rightWarehouseContainer);
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

        foreach (var kv in _tempCurrentChords)
        {
            if (kv.Key == ChordCategory.Bonus) continue;
            Guitar.ReplaceChord(kv.Key, kv.Value);
        }

        var oldBonus = Guitar.GetBonusChords().ToList();
        for (int i = oldBonus.Count - 1; i >= 0; i--)
            Guitar.RemoveBonusChord(oldBonus[i]);
        foreach (var chordId in _tempBonusChords)
            Guitar.AddBonusChord(chordId);

        Guitar.SyncToSaved();
        SyncIfMultiplayer();
        QueueFree();
    }

    private void OnCancel()
    {
        QueueFree();
    }

    public void SetTempSlot(ChordCategory slotCategory, int bonusIndex, string newChordId)
    {
        if (_readOnly) return;

        if (slotCategory == ChordCategory.Bonus)
        {
            if (bonusIndex >= 0 && bonusIndex < _tempBonusChords.Count)
                _tempBonusChords[bonusIndex] = newChordId;
        }
        else
        {
            _tempCurrentChords[slotCategory] = newChordId;
        }
        RefreshAll();
    }

    public int GetBonusChordIndex(string chordId)
    {
        return _tempBonusChords.IndexOf(chordId);
    }

    public void SyncIfMultiplayer()
    {
        if (Guitar == null) return;
        var netService = RunManager.Instance.NetService;
        if (netService != null && netService.Type != NetGameType.Singleplayer)
        {
            var msg = new ChordSyncMessage
            {
                PlayerNetId = Guitar.Owner.NetId,
                ChordsData = string.Join(";", Guitar.GetCurrentChords()
                    .Where(kv => kv.Key != ChordCategory.Bonus)
                    .Select(kv => $"{(int)kv.Key}:{kv.Value}")),
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

        var chords = _readOnly ? Guitar.GetCurrentChords() : _tempCurrentChords;
        var bonusChords = _readOnly ? Guitar.GetBonusChords() : _tempBonusChords;

        if (_readOnly)
        {
            AddStaticSlotRow(new LocString(LocTable, "CUTE_SAKIKO_MOD_SLOT_MAJOR").GetFormattedText(),
                chords.GetValueOrDefault(ChordCategory.Major, ""));
            AddStaticSlotRow(new LocString(LocTable, "CUTE_SAKIKO_MOD_SLOT_MINOR").GetFormattedText(),
                chords.GetValueOrDefault(ChordCategory.Minor, ""));
            AddStaticSlotRow(new LocString(LocTable, "CUTE_SAKIKO_MOD_SLOT_DOMINANT").GetFormattedText(),
                chords.GetValueOrDefault(ChordCategory.Dominant, ""));

            for (int i = 0; i < bonusChords.Count; i++)
            {
                var baseText = new LocString(LocTable, "CUTE_SAKIKO_MOD_SLOT_BONUS").GetFormattedText();
                AddStaticSlotRow($"{baseText} {i + 1}", bonusChords[i]);
            }
        }
        else
        {
            AddSlotRow(new LocString(LocTable, "CUTE_SAKIKO_MOD_SLOT_MAJOR").GetFormattedText(), ChordCategory.Major,
                chords.GetValueOrDefault(ChordCategory.Major, ""));
            AddSlotRow(new LocString(LocTable, "CUTE_SAKIKO_MOD_SLOT_MINOR").GetFormattedText(), ChordCategory.Minor,
                chords.GetValueOrDefault(ChordCategory.Minor, ""));
            AddSlotRow(new LocString(LocTable, "CUTE_SAKIKO_MOD_SLOT_DOMINANT").GetFormattedText(), ChordCategory.Dominant,
                chords.GetValueOrDefault(ChordCategory.Dominant, ""));

            for (int i = 0; i < bonusChords.Count; i++)
            {
                var baseText = new LocString(LocTable, "CUTE_SAKIKO_MOD_SLOT_BONUS").GetFormattedText();
                AddSlotRow($"{baseText} {i + 1}", ChordCategory.Bonus, bonusChords[i]);
            }
        }
    }

    private void AddSlotRow(string labelText, ChordCategory slotCategory, string currentChordId)
    {
        var hbox = new HBoxContainer();
        var label = new Label { Text = labelText, CustomMinimumSize = new Vector2(130, 40) };
        hbox.AddChild(label);
        var dropArea = new ChordSlotDropTarget(slotCategory, currentChordId, this);
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
                    var tip = ChordDisplayHelper.GetDynamicChordHoverTip(chordId, Guitar.Owner.Creature, Guitar.GetEffectMultiplier());
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

            var grid = new GridContainer { Columns = Mathf.Max(1, 4) };
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
                            var tip = ChordDisplayHelper.GetDynamicChordHoverTip(chord.Id, Guitar.Owner.Creature, Guitar.GetEffectMultiplier());
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
        if (pos.X + tipSet.Size.X > viewportRect.Size.X - 10)
            pos.X = viewportRect.Size.X - tipSet.Size.X - 10;
        if (pos.Y < 10) pos.Y = 10;
        if (pos.Y + tipSet.Size.Y > viewportRect.Size.Y - 10)
            pos.Y = viewportRect.Size.Y - tipSet.Size.Y - 10;
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