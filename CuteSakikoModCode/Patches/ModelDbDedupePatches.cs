using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Patches
{
    public static class ModelDbDedupePatches
    {
        public static void Apply()
        {
            var harmony = new Harmony("com.cutesakiko.modeldb.dedupe");

            // 去重角色遗物池（图鉴用）
            harmony.Patch(
                original: AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllCharacterRelicPools)),
                postfix: new HarmonyMethod(typeof(ModelDbDedupePatches), nameof(DedupeRelicPostfix))
            );

            // 去重角色药水池（图鉴用）
            harmony.Patch(
                original: AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllCharacterPotionPools)),
                postfix: new HarmonyMethod(typeof(ModelDbDedupePatches), nameof(DedupePotionPostfix))
            );
        }

        private static void DedupeRelicPostfix(ref IEnumerable<RelicPoolModel> __result)
        {
            if (__result != null)
                __result = __result.Distinct();
        }

        private static void DedupePotionPostfix(ref IEnumerable<PotionPoolModel> __result)
        {
            if (__result != null)
                __result = __result.Distinct();
        }
    }
}