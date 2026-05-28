using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch]
public static class PigAnimationPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.TriggerAnim))]
    public static void OnTriggerAnim(Creature creature, string triggerName, float waitTime)
    {
        if (!PigPower.PigAnimPlayers.TryGetValue(creature, out var animPlayer))
            return;

        if (animPlayer == null || !GodotObject.IsInstanceValid(animPlayer))
        {
            PigPower.PigAnimPlayers.Remove(creature);
            return;
        }

        var animName = triggerName switch
        {
            "Idle" => "idle_loop",
            "Attack" => "attack",
            "Cast" => "cast",
            "Hit" => "hurt",
            _ => null
        };

        if (string.IsNullOrEmpty(animName) || !animPlayer.HasAnimation(animName))
            return;

        animPlayer.Play(animName);

        // ✅ 确保非 idle 动画结束后回到 idle_loop
        if (animName != "idle_loop" && animName != "die" && animPlayer.HasAnimation("idle_loop"))
            animPlayer.Queue("idle_loop");
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
    public static void OnStartDeathAnim(NCreature __instance)
    {
        var creature = __instance.Entity;
        if (!PigPower.PigAnimPlayers.TryGetValue(creature, out var animPlayer))
            return;

        if (animPlayer == null || !GodotObject.IsInstanceValid(animPlayer))
        {
            PigPower.PigAnimPlayers.Remove(creature);
            return;
        }

        if (animPlayer.HasAnimation("die"))
            animPlayer.Play("die");
    }
}