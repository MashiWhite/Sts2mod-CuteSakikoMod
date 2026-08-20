using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace CuteSakikoMod.CuteSakikoModCode.Patches.Animation;

[HarmonyPatch]
public static class ObAnimationPatch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(CreatureCmd), nameof(CreatureCmd.TriggerAnim))]
    public static void OnTriggerAnim(Creature creature, string triggerName, float waitTime)
    {
        if (!ObCardPower.ObAnimPlayers.TryGetValue(creature, out var animPlayer))
            return;

        if (animPlayer == null || !GodotObject.IsInstanceValid(animPlayer))
        {
            ObCardPower.ObAnimPlayers.Remove(creature);
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

        // 播放目标动画
        animPlayer.Play(animName);

        // 确保攻击、施法、受伤动画结束后自动回到 idle_loop
        if (animName != "idle_loop" && animName != "die" && animPlayer.HasAnimation("idle_loop"))
            animPlayer.Queue("idle_loop");
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NCreature), nameof(NCreature.StartDeathAnim))]
    public static void OnStartDeathAnim(NCreature __instance)
    {
        var creature = __instance.Entity;
        if (!ObCardPower.ObAnimPlayers.TryGetValue(creature, out var animPlayer))
            return;

        if (animPlayer == null || !GodotObject.IsInstanceValid(animPlayer))
        {
            ObCardPower.ObAnimPlayers.Remove(creature);
            return;
        }

        if (animPlayer.HasAnimation("die"))
            animPlayer.Play("die");
    }
}