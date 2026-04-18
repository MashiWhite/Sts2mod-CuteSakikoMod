using System.Collections;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace CuteSakikoMod.CuteSakikoModCode.Others;

public class NDeadAnimation
{
    [HarmonyPatch("MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen.NGameOverScreen", "AfterOverlayOpened")]
    public static class NGameOverScreenAfterOverlayOpenedPatch
    {
        private static bool Prefix(object __instance)
        {
            // 反射获取 _runState，判断是否为自定义角色
            object? runState = null;
            IEnumerable? players = null;
            var isSaki = false;
            try
            {
                var runStateField = __instance.GetType().GetField("_runState",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                runState = runStateField?.GetValue(__instance);
                if (runState != null)
                {
                    var playersProp = runState.GetType().GetProperty("Players",
                        BindingFlags.Public | BindingFlags.Instance);
                    players = playersProp?.GetValue(runState) as IEnumerable;
                    if (players != null)
                        foreach (var p in players)
                        {
                            var charProp = p.GetType().GetProperty("Character",
                                BindingFlags.Public | BindingFlags.Instance);
                            if (charProp?.GetValue(p) is Character.CuteSaki)
                            {
                                isSaki = true;
                                break;
                            }
                        }
                }
            }
            catch
            {
            }

            if (isSaki) PlayDeathAnimation();

            return true;
        }

        private static void PlayDeathAnimation()
        {
            var combatRoom = NCombatRoom.Instance;
            if (combatRoom == null) return;

            foreach (var child in combatRoom.GetChildren())
                if (child is NCreature nCreature && nCreature.Entity?.Player?.Character is Character.CuteSaki)
                {
                    var animPlayer = nCreature.GetNodeOrNull<AnimationPlayer>("Visuals/AnimationPlayer");
                    if (animPlayer != null && animPlayer.HasAnimation("die"))
                    {
                        animPlayer.Play("die");
                        break;
                    }
                }
        }
    }
}