using CuteSakikoMod.CuteSakikoModCode.Others;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch]
public static class StrikeDummyPatch
{
    [HarmonyPatch(typeof(StrikeDummy), "ModifyDamageAdditive")]
    [HarmonyPostfix]
    public static void StrikeDummy_Postfix(StrikeDummy __instance, ref decimal __result, Creature? target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (__result != 0m) return;
        if (!props.IsPoweredAttack()) return;
        if (cardSource == null) return;
        if (cardSource.HasModKeyword(CutesakiKeywords.Playpiano) ||
            cardSource.HasModKeyword(CutesakiKeywords.Playguitar))
            __result = __instance.DynamicVars["ExtraDamage"].BaseValue;
    }

    [HarmonyPatch(typeof(FakeStrikeDummy), "ModifyDamageAdditive")]
    [HarmonyPostfix]
    public static void FakeStrikeDummy_Postfix(FakeStrikeDummy __instance, ref decimal __result, Creature? target,
        decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (__result != 0m) return;
        if (!props.IsPoweredAttack()) return;
        if (cardSource == null) return;
        if (cardSource.HasModKeyword(CutesakiKeywords.Playpiano) ||
            cardSource.HasModKeyword(CutesakiKeywords.Playguitar))
            __result = __instance.DynamicVars["ExtraDamage"].BaseValue;
    }
}