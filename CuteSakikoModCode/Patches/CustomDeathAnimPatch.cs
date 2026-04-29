using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
public static class CustomDeathAnimPatch
{
    public static void Postfix(NCreature __instance)
    {
        // 只处理玩家角色，且没有 Spine 动画（即使用 AnimationPlayer 的角色）
        if (!__instance.Entity.IsPlayer) return;
        if (__instance.HasSpineAnimation) return; // 有 Spine 的角色走原版逻辑

        var visuals = __instance.Visuals;
        if (visuals == null) return;

        // 在 Visuals 子节点中寻找 AnimationPlayer
        var ap = FindAnimationPlayer(visuals);
        if (ap != null && ap.HasAnimation("die")) TaskHelper.RunSafely(PlayDeathAnimationAsync(ap));
    }

    private static async Task PlayDeathAnimationAsync(AnimationPlayer ap)
    {
        await Task.Delay(16); // 等待一帧
        if (!GodotObject.IsInstanceValid(ap)) return;

        ap.Play("die");
        while (ap.IsPlaying() && GodotObject.IsInstanceValid(ap))
            await Task.Delay(16);
    }

    private static AnimationPlayer FindAnimationPlayer(Node node)
    {
        if (node is AnimationPlayer ap) return ap;
        foreach (var child in node.GetChildren())
        {
            var found = FindAnimationPlayer(child);
            if (found != null) return found;
        }

        return null;
    }
}