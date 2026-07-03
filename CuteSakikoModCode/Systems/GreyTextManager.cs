using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
public static class GreyTextPatch
{
    public static void Postfix(NCombatRoom __instance)
    {
        GreyTextManager.Setup(__instance);
    }
}

public static class GreyTextManager
{
    private static Node2D? _container;
    private static readonly List<MegaRichTextLabel> _pool = new();
    private static Font? _font;

    public static void Setup(NCombatRoom room)
    {
        if (_container != null && GodotObject.IsInstanceValid(_container))
            return;

        _container = new Node2D
        {
            Name = "GreyTextContainer",
            ZIndex = 100   // 确保在最顶层，不被战斗 UI 遮挡
        };
        room.AddChild(_container);
        _font = ThemeDB.FallbackFont;
    }

    public static void Spawn(string text, Vector2 position, string? audioPath = null)
    {
        if (_container == null) return;

        var label = GetOrCreateLabel();
        // 先设置完整文本
        label.Text = text;
        // ★ 关键：在标签进入场景树之前，就将可见字符数归零，防止第一帧渲染全部文字
        label.VisibleCharacters = 0;
        label.Visible = true;
        label.Modulate = Colors.White;
        label.Position = position;

        _container.AddChild(label);
        _ = AnimateText(label, text, position, audioPath);
    }

    private static async Task AnimateText(MegaRichTextLabel label, string fullText, Vector2 basePosition, string? audioPath)
    {
        const float charDelay = 0.03f;
        const float floatDuration = 1.2f;
        const float floatDistance = 50f;
        const float stayDuration = 1.5f;

        // 等待一帧，让 BBCode 解析完成（此时 VisibleCharacters 已经是 0，不会显示文字）
        await Task.Yield();
        if (!GodotObject.IsInstanceValid(label)) return;

        // 打字机效果：逐字增加 VisibleCharacters
        int totalChars = fullText.Length;
        for (int i = 0; i < totalChars; i++)
        {
            label.VisibleCharacters = i + 1;
            label.Position = basePosition + new Vector2(
                (float)GD.RandRange(-2.0, 2.0),
                (float)GD.RandRange(-2.0, 2.0));
            await Task.Delay((int)(charDelay * 1000));
            if (!GodotObject.IsInstanceValid(label)) return;
        }

        label.Position = basePosition;

        // 停留
        await Task.Delay((int)(stayDuration * 1000));
        if (!GodotObject.IsInstanceValid(label)) return;

        // 浮动淡出
        var tween = _container?.CreateTween();
        if (tween == null) return;

        tween.TweenProperty(label, "position:y", label.Position.Y - floatDistance, floatDuration);
        tween.Parallel().TweenProperty(label, "modulate:a", 0.0f, floatDuration);

        await Task.Delay((int)(floatDuration * 1000) + 50);
        if (!GodotObject.IsInstanceValid(label)) return;

        // 回收
        label.Visible = false;
        label.GetParent()?.RemoveChild(label);
    }

    private static MegaRichTextLabel GetOrCreateLabel()
    {
        foreach (var label in _pool)
        {
            if (!label.Visible)
            {
                label.GetParent()?.RemoveChild(label);
                return label;
            }
        }

        var newLabel = CreateLabel();
        _pool.Add(newLabel);
        return newLabel;
    }

    private static MegaRichTextLabel CreateLabel()
    {
        var label = new MegaRichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = false,           // 必须关闭，否则与 AutoSizeEnabled 冲突
            AutoSizeEnabled = false,      // 关闭自动字号
            ScrollActive = false,
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        // 固定一个较大的尺寸，保证所有台词都能完整显示
        label.Size = new Vector2(800, 120);

        // 样式
        label.AddThemeFontSizeOverride("normal_font_size", 35);
        if (_font != null)
            label.AddThemeFontOverride("normal_font", _font);

        label.AddThemeColorOverride("default_color", new Color(1, 1, 1, 1));
        label.AddThemeColorOverride("font_shadow_color", new Color(0, 0, 0, 0.6f));
        label.AddThemeConstantOverride("shadow_outline_size", 2);
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 2);

        return label;
    }
}