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
        if (_container != null && GodotObject.IsInstanceValid(_container) && _container.GetParent() == room)
            return;

        if (_container != null && GodotObject.IsInstanceValid(_container))
        {
            _container.QueueFree();
        }

        _container = new Node2D
        {
            Name = "GreyTextContainer",
            ZIndex = 100
        };
        room.AddChild(_container);
        _font = ThemeDB.FallbackFont;
        GD.Print("[GreyTextManager] Setup complete for room: " + room.Name);
    }

    public static void Spawn(string text, Vector2 position, string? audioPath = null)
    {
        EnsureContainer();

        if (_container == null || !GodotObject.IsInstanceValid(_container))
        {
            GD.PrintErr("[GreyTextManager] Container invalid, cannot spawn.");
            return;
        }

        // ★ 添加粗体 BBCode，不破坏原有颜色标签
        string boldText = "[font_size=38][b]" + text + "[/b][/font_size]";

        var label = GetOrCreateLabel();
        label.Text = boldText;
        label.VisibleCharacters = 0;
        label.Visible = true;
        label.Modulate = Colors.White;
        label.Position = position;

        _container.AddChild(label);
        _ = AnimateText(label, boldText, position, audioPath);
    }

    private static void EnsureContainer()
    {
        if (_container != null && GodotObject.IsInstanceValid(_container))
        {
            var currentRoom = NCombatRoom.Instance;
            if (currentRoom != null && _container.GetParent() == currentRoom)
                return;
        }

        var room = NCombatRoom.Instance;
        if (room != null)
        {
            Setup(room);
        }
        else
        {
            GD.PrintErr("[GreyTextManager] Cannot ensure container: NCombatRoom.Instance is null.");
        }
    }

    private static async Task AnimateText(MegaRichTextLabel label, string fullText, Vector2 basePosition, string? audioPath)
    {
        const float charDelay = 0.03f;
        const float floatDuration = 1.2f;
        const float floatDistance = 50f;
        const float stayDuration = 1.5f;

        await Task.Yield();
        if (!GodotObject.IsInstanceValid(label)) return;

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

        await Task.Delay((int)(stayDuration * 1000));
        if (!GodotObject.IsInstanceValid(label)) return;

        var tween = _container?.CreateTween();
        if (tween == null) return;

        tween.TweenProperty(label, "position:y", label.Position.Y - floatDistance, floatDuration);
        tween.Parallel().TweenProperty(label, "modulate:a", 0.0f, floatDuration);

        await Task.Delay((int)(floatDuration * 1000) + 50);
        if (!GodotObject.IsInstanceValid(label)) return;

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
            FitContent = false,
            AutoSizeEnabled = false,
            ScrollActive = false,
            Visible = false,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        label.Size = new Vector2(800, 120);

        label.AddThemeFontSizeOverride("normal_font_size", 80);
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