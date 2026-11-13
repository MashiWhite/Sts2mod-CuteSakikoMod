using System.Collections.Generic;
using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Nodes.Combat;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace CuteSakikoMod.CuteSakikoModCode.Patches.Dread;

/// <summary>
///     让自定义计数意图（飞返打出次数 / 读档次数 / 翻倍）显示我们自己的图标与数字。
///     原版 NIntent.UpdateVisuals 的图标来自 IntentAnimData 图集帧，数字只对
///     AttackIntent / StatusIntent 显示；这里对自定义意图直接接管贴图与文本，
///     并替换掉节点复用时残留的动画帧（否则 _Process 会用上一只怪的 buff 帧覆盖图标）。
/// </summary>
[HarmonyPatch(typeof(NIntent), "UpdateVisuals")]
public static class NIntentCustomCountIntentPatch
{
    private static readonly FieldInfo? IntentField = AccessTools.Field(typeof(NIntent), "_intent");
    private static readonly FieldInfo? TargetsField = AccessTools.Field(typeof(NIntent), "_targets");
    private static readonly FieldInfo? OwnerField = AccessTools.Field(typeof(NIntent), "_owner");
    private static readonly FieldInfo? SpriteField = AccessTools.Field(typeof(NIntent), "_intentSprite");
    private static readonly FieldInfo? ValueLabelField = AccessTools.Field(typeof(NIntent), "_valueLabel");
    private static readonly FieldInfo? AnimationFramesField = AccessTools.Field(typeof(NIntent), "_animationFrames");
    private static readonly FieldInfo? AnimationFrameField = AccessTools.Field(typeof(NIntent), "_animationFrame");
    private static readonly FieldInfo? AnimationNameField = AccessTools.Field(typeof(NIntent), "_animationName");

    [HarmonyPriority(Priority.First)]
    private static bool Prefix(NIntent __instance)
    {
        if (IntentField?.GetValue(__instance) is not CustomCountIntent intent) return true;

        var targets = TargetsField?.GetValue(__instance) as IEnumerable<Creature>
                      ?? System.Array.Empty<Creature>();
        var owner = OwnerField?.GetValue(__instance) as Creature;
        var texture = intent.IconTexture;

        if (SpriteField?.GetValue(__instance) is Sprite2D sprite && texture != null)
            sprite.Texture = texture;

        // ★ 意图节点会被复用：把旧动画帧换掉，否则 _Process 会用上一只怪的 buff 帧覆盖我们的图标
        if (AnimationFramesField?.GetValue(__instance) is List<Texture2D> frames)
        {
            frames.Clear();
            if (texture != null) frames.Add(texture);
        }

        AnimationFrameField?.SetValue(__instance, 0);
        AnimationNameField?.SetValue(__instance, "__custom_count_intent__");

        if (ValueLabelField?.GetValue(__instance) is RichTextLabel label)
        {
            label.Text = owner == null || !intent.ShowCount
                ? string.Empty
                : intent.GetIntentLabel(targets, owner).GetFormattedText() ?? string.Empty;
        }

        return false;
    }
}
