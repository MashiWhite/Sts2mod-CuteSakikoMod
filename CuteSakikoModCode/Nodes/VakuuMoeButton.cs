using System;
using System.Collections.Generic;
using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Others.Telemetry;
using Godot;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes;

public partial class VakuuMoeButton : NButton
{
    private bool _alreadyUsed;
    private AncientEventModel? _eventModel;

    // 反射缓存：AncientEventModel 的私有字段 _generatedOptions
    private static readonly FieldInfo? GeneratedOptionsField =
        typeof(AncientEventModel).GetField("_generatedOptions",
            BindingFlags.NonPublic | BindingFlags.Instance);

    public override void _Ready()
    {
        ConnectSignals();

        var eventRoom = NEventRoom.Instance;
        if (eventRoom != null)
        {
            var eventField = typeof(NEventRoom).GetField("_event", BindingFlags.NonPublic | BindingFlags.Instance);
            _eventModel = eventField?.GetValue(eventRoom) as AncientEventModel;
        }

        // 不是自定义角色就不显示
        if (!IsModCharacter(_eventModel?.Owner?.Character))
        {
            QueueFree();
            return;
        }

        var buttonWidth = 180f;
        var buttonHeight = 180f;
        AnchorLeft = 1.0f;
        AnchorRight = 1.0f;
        AnchorTop = 0.0f;
        AnchorBottom = 0.0f;
        OffsetRight = 0;
        OffsetLeft = -buttonWidth;
        OffsetTop = 750;
        OffsetBottom = OffsetTop + buttonHeight;

        var texture = GD.Load<Texture2D>("res://CuteSakikoMod/images/others/others/vakuu_love_icon.png");
        var img = new TextureRect();
        img.Texture = texture;
        img.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
        img.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        img.MouseFilter = MouseFilterEnum.Ignore;
        img.AnchorRight = 1.0f;
        img.AnchorBottom = 1.0f;
        img.OffsetRight = 0;
        img.OffsetLeft = 0;
        img.OffsetTop = 0;
        img.OffsetBottom = 0;
        AddChild(img);

        Visible = true;
    }

    private static bool IsModCharacter(CharacterModel? character)
    {
        if (character == null) return false;
        var type = character.GetType();
        var target = typeof(CuteSakikoCharacter<,,>);
        while (type != null && type != typeof(CharacterModel))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == target)
                return true;
            type = type.BaseType;
        }
        return false;
    }

    protected override void OnFocus()
    {
        base.OnFocus();
        var title = new LocString("events", "CUTE_SAKIKO_MOD_VAKUU_BUTTON.title");
        var desc = new LocString("events", "CUTE_SAKIKO_MOD_VAKUU_BUTTON.description");
        var tip = new HoverTip(title, desc);
        var tipSet = NHoverTipSet.CreateAndShow(this, tip);
        if (tipSet == null) return;
        var alignment = GlobalPosition.X > GetViewportRect().Size.X * 0.6f
            ? HoverTipAlignment.Left
            : HoverTipAlignment.Right;
        tipSet.SetAlignment(this, alignment);
        var viewportRect = GetViewportRect();
        var pos = tipSet.GlobalPosition;
        if (pos.X < 0) pos.X = 10;
        if (pos.X + tipSet.Size.X > viewportRect.Size.X) pos.X = viewportRect.Size.X - tipSet.Size.X - 10;
        if (pos.Y < 0) pos.Y = 10;
        if (pos.Y + tipSet.Size.Y > viewportRect.Size.Y) pos.Y = viewportRect.Size.Y - tipSet.Size.Y - 10;
        tipSet.GlobalPosition = pos;
    }

    protected override void OnUnfocus()
    {
        NHoverTipSet.Remove(this);
        base.OnUnfocus();
    }

    protected override void OnRelease()
    {
        base.OnRelease();
        if (_eventModel == null) return;

        // 第一次点击：给予奖励 + 结束事件 + 上传遥测
        if (!_alreadyUsed && !_eventModel.IsFinished)
        {
            _alreadyUsed = true;

            var player = _eventModel.Owner;
            if (player != null)
            {
                var creature = player.Creature;
                creature.SetMaxHpInternal(creature.MaxHp + 5);
                creature.SetCurrentHpInternal(creature.CurrentHp + 5);

                // 抓取当前事件房里所有被跳过的选项
                var (skippedKeys, skippedNames) = CollectSkippedOptions();

                try
                {
                    CuteSakikoModTelemetry.CaptureRoomButtonClicked(
                        roomType: "event",
                        buttonId: "vakuu_moe",
                        characterId: player.Character.Id.Entry,
                        floor: CuteSakikoModTelemetry.GetCurrentFloor(player),
                        extra: new Dictionary<string, object?>
                        {
                            ["hp_gain"] = 5,
                            ["event_id"] = _eventModel.Id.Entry,
                            ["event_name"] = CuteSakikoModTelemetry.LocalizeEvent(_eventModel.Id.Entry),
                            ["skipped_keys"] = string.Join(",", skippedKeys),
                            ["skipped_names"] = string.Join(",", skippedNames),
                            ["skipped_count"] = skippedKeys.Count,
                        });
                }
                catch (Exception ex)
                {
                    GD.PrintErr($"[VakuuMoe] Telemetry failed: {ex}");
                }
            }

            // 正常结束事件
            var doneMethod = typeof(AncientEventModel).GetMethod("Done", BindingFlags.NonPublic | BindingFlags.Instance);
            doneMethod?.Invoke(_eventModel, null);
        }

        // 每次点击都打开地图
        NMapScreen.Instance.SetTravelEnabled(true);
        NMapScreen.Instance.Open();
    }

    /// <summary>
    /// 抓取当前古代事件房里的所有选项（点击按钮等于跳过整间）。
    /// 返回 (本地化键列表, 显示名列表)。
    /// </summary>
    private (List<string> keys, List<string> names) CollectSkippedOptions()
    {
        var keys = new List<string>();
        var names = new List<string>();
        if (_eventModel == null) return (keys, names);

        try
        {
            if (GeneratedOptionsField?.GetValue(_eventModel) is System.Collections.IEnumerable raw)
            {
                foreach (var obj in raw)
                {
                    if (obj is not EventOption opt) continue;

                    var key = opt.TextKey ?? "";
                    if (string.IsNullOrEmpty(key)) continue;

                    var title = "";
                    try { title = opt.Title?.GetFormattedText() ?? ""; } catch { }

                    keys.Add(key);
                    names.Add(title);
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"[VakuuMoe] CollectSkippedOptions failed: {ex.Message}");
        }

        return (keys, names);
    }
}