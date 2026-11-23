using System.Collections.Generic;
using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.Encounters;
using CuteSakikoMod.CuteSakikoModCode.Others.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

/// <summary>
///     关闭「模组怪物」时从 Act 的遭遇列表里彻底过滤 mod 遭遇。
///     CuteEncounters.IsValidForAct 只影响普通遭遇池；Boss 选择直接读 AllBossEncounters，
///     所以必须在这里把 Boss 也过滤掉，否则关掉设置仍会抽到天祥罗等 mod Boss。
/// </summary>
[HarmonyPatch(typeof(ActModel), nameof(ActModel.AllEncounters), MethodType.Getter)]
public static class ModEncountersFilterPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        if (ModConfig.EnableModMonsters) return;
        __result = __result.Where(e => e is not CuteEncounters).ToList();
    }
}

[HarmonyPatch(typeof(ActModel), nameof(ActModel.AllBossEncounters), MethodType.Getter)]
public static class ModBossEncountersFilterPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref IEnumerable<EncounterModel> __result)
    {
        if (ModConfig.EnableModMonsters) return;
        __result = __result.Where(e => e is not CuteEncounters).ToList();
    }
}
