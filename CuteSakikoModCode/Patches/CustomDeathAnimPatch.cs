using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(NCreature), "StartDeathAnim")]
public static class CustomDeathAnimPatch
{
    public static void Postfix(NCreature __instance)
    {
        if (!__instance.Entity.IsPlayer) return;
        var visuals = __instance.Visuals;
        if (visuals == null) return;
        TaskHelper.RunSafely(PlayDeathAnimationAsync(visuals));
    }

    private static async Task PlayDeathAnimationAsync(Node visuals)
    {
        // 等待一帧（约16ms），确保节点稳定
        await Task.Delay(16);
        if (!GodotObject.IsInstanceValid(visuals)) return;

        var animationPlayer = FindAnimationPlayer(visuals);
        if (animationPlayer == null) return;

        if (animationPlayer.HasAnimation("die"))
        {
            animationPlayer.Play("die");
            // 等待动画结束（轮询检查）
            await WaitForAnimationFinish(animationPlayer);
        }
    }

    private static async Task WaitForAnimationFinish(AnimationPlayer animPlayer)
    {
        while (animPlayer.IsPlaying()) await Task.Delay(16); // 每帧检查一次
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