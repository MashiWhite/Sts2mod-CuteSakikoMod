using Godot;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Core;
using CuteSakikoMod.CuteSakikoModCode.Systems.Skin.Ui;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Dread;

/// <summary>
///     恐惧模式（NDreadScreen）没有原版角色描述框，这里从原版角色选择场景里
///     复制一份 InfoPanel（名字 / HP / 金币 / 描述 / 起始遗物），挂到 Dread 界面上。
/// </summary>
public static class DreadCharacterInfoPanel
{
    private const string SourceScenePath = "res://scenes/screens/character_select_screen.tscn";
    private const string PanelNodePath = "InfoPanel";

    private static Control? _panel;

    /// <summary>把描述框挂到 Dread 界面（只挂一次）。返回是否可用。</summary>
    public static Control? EnsureAttached(Control dreadScreen)
    {
        var existing = dreadScreen.GetNodeOrNull<Control>("DreadInfoPanel");
        if (existing != null) { _panel = existing; return existing; }
        if (_panel != null && GodotObject.IsInstanceValid(_panel)) return _panel;

        var packed = ResourceLoader.Load<PackedScene>(SourceScenePath);
        if (packed == null)
        {
            Entry.Logger.Warn($"[DreadInfo] 找不到原版角色选择场景：{SourceScenePath}");
            return null;
        }

        var root = packed.Instantiate();
        var panel = root.GetNodeOrNull<Control>(PanelNodePath);
        if (panel == null)
        {
            Entry.Logger.Warn($"[DreadInfo] 原版场景里没有 {PanelNodePath}");
            root.Free();
            return null;
        }

        root.RemoveChild(panel);
        root.Free();

        panel.Name = "DreadInfoPanel";
        dreadScreen.AddChild(panel);
        _panel = panel;
        // 通知卡组/遗物切换面板：InfoPanel 出现了，重新定位（恐惧模式下面板晚于它们创建）
        SkinSystemEvents.RaiseCurrentCharacterChanged(SkinUiContext.CurrentCharacterType);
        return panel;
    }

    /// <summary>按角色刷新描述框内容（对齐 Blitz 的 InfoPanel 逻辑）。</summary>
    public static void Bind(CharacterModel? character)
    {
        var panel = _panel;
        if (panel == null || !GodotObject.IsInstanceValid(panel)) return;

        // 原版角色 / 其他 mod 角色：不改他们的选择界面，直接隐藏
        if (character == null || !CharacterSkinRegistry.HasSkins(character.GetType()))
        {
            panel.Visible = false;
            return;
        }

        panel.Visible = true;

        var name = panel.GetNodeOrNull<Label>("VBoxContainer/Name");
        var desc = panel.GetNodeOrNull<RichTextLabel>("VBoxContainer/DescriptionLabel");
        var hp = panel.GetNodeOrNull<Label>("VBoxContainer/HpGoldSpacer/HpGold/Hp/Label");
        var gold = panel.GetNodeOrNull<Label>("VBoxContainer/HpGoldSpacer/HpGold/Gold/Label");
        var relicTitle = panel.GetNodeOrNull<RichTextLabel>("VBoxContainer/Relic/Name/RichTextLabel");
        var relicDesc = panel.GetNodeOrNull<RichTextLabel>("VBoxContainer/Relic/Description");
        var relicIcon = panel.GetNodeOrNull<TextureRect>("VBoxContainer/Relic/Icon");

        name?.Set("text", new LocString("characters", character.CharacterSelectTitle).GetFormattedText());
        desc?.Set("text", new LocString("characters", character.CharacterSelectDesc).GetFormattedText());
        hp?.Set("text", $"{character.StartingHp}/{character.StartingHp}");
        gold?.Set("text", $"{character.StartingGold}");

        var relics = character.StartingRelics;
        if (relics is { Count: > 0 })
        {
            var relic = relics[0];
            relicTitle?.Set("text", relic.Title.GetFormattedText());
            relicDesc?.Set("text", relic.DynamicDescription.GetFormattedText());
            if (relicIcon != null)
            {
                relicIcon.Texture = relic.Icon;
                relicIcon.SelfModulate = Colors.White;
            }
        }

        // MegaLabel 需要手动触发一次自适应字号
        name?.Call("SetTextAutoSize", name.Text);
        hp?.Call("SetTextAutoSize", hp.Text);
        gold?.Call("SetTextAutoSize", gold.Text);
    }
}
