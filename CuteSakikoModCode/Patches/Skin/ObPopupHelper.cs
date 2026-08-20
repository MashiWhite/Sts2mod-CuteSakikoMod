using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using System;

namespace CuteSakikoMod.CuteSakikoModCode.Patches.Skin;

internal static class ObPopupHelper
{
    private const string PopupButtonName = "ObPopupButton";
    private const float AnimationDuration = 0.25f;
    private const float RightMargin = 50f;

    private static NCharacterSelectButton? _popup;
    private static NCharacterSelectScreen? _screen;
    private static NCharacterSelectButton? _sakiButton;
    private static bool _obSelected;
    private static bool _popupVisible;
    private static Tween? _moveTween;

    // 移除静态 PackedScene 缓存，每次在 Setup 中动态加载，确保资源有效

    private static PackedScene? LoadButtonScene()
    {
        // 使用 ResourceLoader.Load 每次重新加载，避免使用被释放的旧引用
        return ResourceLoader.Load<PackedScene>("res://scenes/screens/char_select/char_select_button.tscn");
    }

    public static void PreloadButtonScene()
    {
        // 仅用于预加载，但实际上我们会在 Setup 中重新加载
        // 保留此方法以防其他地方调用，但内部不做缓存
    }

    public static void Setup(NCharacterSelectScreen screen)
    {
        if (screen == null || GodotObject.IsInstanceValid(screen) == false)
            return;

        _screen = screen;
        _obSelected = false;
        _popupVisible = false;
        _moveTween?.Kill();

        // 清理旧 popup
        var oldPopup = screen.GetNodeOrNull<NCharacterSelectButton>(PopupButtonName);
        if (oldPopup != null)
        {
            oldPopup.QueueFree();
        }

        // ★ 关键修复：每次重新加载场景资源，避免使用已释放的旧引用
        var buttonScene = LoadButtonScene();
        if (buttonScene == null || !GodotObject.IsInstanceValid(buttonScene))
        {
            // 加载失败，记录错误并返回
            GD.PushError("[ObPopupHelper] Failed to load char_select_button.tscn");
            return;
        }

        NCharacterSelectButton? popup = null;
        try
        {
            popup = buttonScene.Instantiate<NCharacterSelectButton>();
        }
        catch (Exception ex)
        {
            GD.PushError($"[ObPopupHelper] Instantiate failed: {ex.Message}");
            return;
        }

        if (popup == null)
        {
            GD.PushError("[ObPopupHelper] Instantiated popup is null");
            return;
        }

        popup.Name = PopupButtonName;
        screen.AddChild(popup);
        popup.Init(ModelDb.Character<CuteOb>(), screen);
        popup.DebugUnlock(); // 确保可点击
        popup.Visible = false;
        popup.TopLevel = true;
        popup.MouseFilter = Control.MouseFilterEnum.Ignore;
        popup.ZIndex = 100;
        _popup = popup;

        // 查找 Saki 按钮
        _sakiButton = FindSakiButton(screen);

        // 初始位置：屏幕右侧外
        popup.SetGlobalPosition(GetOffscreenRightPosition(screen));
    }

    private static Vector2 GetOffscreenRightPosition(NCharacterSelectScreen screen)
    {
        float x = screen.Size.X + 100;
        float y = _sakiButton != null ? _sakiButton.GlobalPosition.Y : screen.Size.Y * 0.5f;
        return new Vector2(x, y);
    }

    private static Vector2 GetTargetPosition(NCharacterSelectScreen screen)
    {
        float x = screen.Size.X - RightMargin - (_popup?.Size.X ?? 100);
        float y = _sakiButton != null ? _sakiButton.GlobalPosition.Y : screen.Size.Y * 0.5f;
        return new Vector2(x, y);
    }

    private static NCharacterSelectButton? FindSakiButton(NCharacterSelectScreen screen)
    {
        var container = screen.GetNodeOrNull<Control>("CharSelectButtons/ButtonContainer");
        if (container == null) return null;
        foreach (var child in container.GetChildren())
        {
            if (child is NCharacterSelectButton btn && btn.Character is CuteSaki)
                return btn;
        }
        return null;
    }

    private static void SlideIn()
    {
        if (_popup == null || _screen == null || _popupVisible || !GodotObject.IsInstanceValid(_popup))
            return;

        _moveTween?.Kill();
        _popupVisible = true;
        _popup.Visible = true;
        _popup.MouseFilter = Control.MouseFilterEnum.Stop;

        Vector2 target = GetTargetPosition(_screen);
        _popup.SetGlobalPosition(GetOffscreenRightPosition(_screen));

        _moveTween = _popup.CreateTween();
        _moveTween.SetEase(Tween.EaseType.Out)
                  .SetTrans(Tween.TransitionType.Quad);
        _moveTween.TweenProperty(_popup, "global_position", target, AnimationDuration);
    }

    private static void SlideOut()
    {
        if (_popup == null || _screen == null || !GodotObject.IsInstanceValid(_popup))
            return;

        _moveTween?.Kill();
        _popupVisible = false;
        _obSelected = false;
        _popup.MouseFilter = Control.MouseFilterEnum.Ignore;

        Vector2 target = GetOffscreenRightPosition(_screen);

        _moveTween = _popup.CreateTween();
        _moveTween.SetEase(Tween.EaseType.Out)
                  .SetTrans(Tween.TransitionType.Quad);
        _moveTween.TweenProperty(_popup, "global_position", target, AnimationDuration);
        _moveTween.Finished += () =>
        {
            if (_popup != null && GodotObject.IsInstanceValid(_popup))
            {
                _popup.Visible = false;
                try { if (_popup.IsSelected) _popup.Deselect(); } catch { }
            }
        };
    }

    public static void OnSakiSelected()
    {
        if (_popupVisible)
        {
            // 按钮已可见：保持位置，重置选中状态
            _obSelected = false;
            try
            {
                if (_popup != null && GodotObject.IsInstanceValid(_popup) && _popup.IsSelected)
                    _popup.Deselect();
            }
            catch { }
            // 不移动
        }
        else
        {
            SlideIn();
        }
    }

    public static void OnObSelected()
    {
        if (_popup == null || !GodotObject.IsInstanceValid(_popup))
            return;
        _obSelected = true;
        _popupVisible = true;
        _moveTween?.Kill();
    }

    public static void OnOtherCharacterSelected()
    {
        if (_popupVisible)
        {
            SlideOut();
        }
        else
        {
            _obSelected = false;
        }
    }

    public static void Cleanup(NCharacterSelectScreen screen)
    {
        _moveTween?.Kill();
        if (_popup != null && GodotObject.IsInstanceValid(_popup))
        {
            _popup.Visible = false;
            _popup.MouseFilter = Control.MouseFilterEnum.Ignore;
        }
        _obSelected = false;
        _popupVisible = false;
    }
}