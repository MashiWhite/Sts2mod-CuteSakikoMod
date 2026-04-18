using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Basic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(TouchOfOrobas))]
public static class TransBaseRelic
{
    [HarmonyPostfix]
    [HarmonyPatch("get_RefinementUpgrades")]
    public static void Postfix(ref Dictionary<ModelId, RelicModel> __result)
    {
        var kabutoNoteId = ModelDb.Relic<KabutoNote>().Id;
        var postItNoteCard = ModelDb.Relic<PostItNote>();


        if (!__result.ContainsKey(kabutoNoteId)) __result[kabutoNoteId] = postItNoteCard;
    }
}