using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(SandpitPower))]
public static class SandpitPowerPatch
{
    private static bool ShouldSkip()
    {
        return MasqueradePower.IsActive;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(SandpitPower.AfterRemoved))]
    public static bool Prefix_AfterRemoved(ref Task __result)
    {
        if (ShouldSkip())
        {
            __result = Task.CompletedTask;
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(SandpitPower.AfterSideTurnStartLate))]
    public static bool Prefix_AfterSideTurnStartLate(ref Task __result)
    {
        if (ShouldSkip())
        {
            __result = Task.CompletedTask;
            return false;
        }

        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(SandpitPower.BeforeSideTurnEnd))]
    public static bool Prefix_BeforeSideTurnEnd(ref Task __result)
    {
        if (ShouldSkip())
        {
            __result = Task.CompletedTask;
            return false;
        }

        return true;
    }
}