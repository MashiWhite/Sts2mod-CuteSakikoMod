using CuteSakikoMod.CuteSakikoModCode.Pools;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(ModelDb), nameof(ModelDb.AllCardPools), MethodType.Getter)]
public static class ModelDbAllCardPoolsPatch
{
    private static void Postfix(ref IEnumerable<CardPoolModel> __result)
    {
        // 将你的自定义卡池添加到结果中
        __result = __result
            .Append(ModelDb.CardPool<CuteSakikoEggCardPool>())
            .Distinct();
    }
}