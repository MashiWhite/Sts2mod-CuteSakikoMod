using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Patches.Speech;

/// <summary>
///     让「坚固外壳」不对「言刃」/「破防」的固定真实伤害生效。
///     外壳走的是 ModifyHpLostBeforeOstyLate（不是 ModifyDamageCap），
///     这里只在 WordBladeDamageScope 激活时放行，不污染 ValueProp，也不影响其他 mod 的伤害。
/// </summary>
[HarmonyPatch(typeof(HardenedShellPower), nameof(HardenedShellPower.ModifyHpLostBeforeOstyLate))]
public static class HardenedShellWordBladeCapPatch
{
    private static bool Prefix(decimal amount, ref decimal __result)
    {
        if (!WordBladeDamageScope.IsActive) return true;
        __result = amount;
        return false;
    }
}

/// <summary>作用域内的伤害不消耗「坚固外壳」的每回合额度，避免影响后续普通伤害。</summary>
[HarmonyPatch(typeof(HardenedShellPower), nameof(HardenedShellPower.AfterDamageReceived))]
public static class HardenedShellWordBladeCounterPatch
{
    private static bool Prefix(ref Task __result)
    {
        if (!WordBladeDamageScope.IsActive) return true;
        // 原方法是 async Task：Prefix 返回 false 时必须补一个非 null Task，
        // 否则 Hook 里 await 到 null 会抛 NullReferenceException。
        __result = Task.CompletedTask;
        return false;
    }
}
