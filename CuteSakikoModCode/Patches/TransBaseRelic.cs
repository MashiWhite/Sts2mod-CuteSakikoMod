using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
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
        // 原有：KabutoNote → PostItNote
        var kabutoNoteId = ModelDb.Relic<KabutoNote>().Id;
        var postItNote = ModelDb.Relic<PostItNote>();
        if (!__result.ContainsKey(kabutoNoteId))
            __result[kabutoNoteId] = postItNote;

        // 新增：AnonGuitar → FlashAnonGuitar
        var anonGuitarId = ModelDb.Relic<AnonGuitar>().Id;
        var flashAnonGuitar = ModelDb.Relic<FlashAnonGuitar>();
        if (!__result.ContainsKey(anonGuitarId))
            __result[anonGuitarId] = flashAnonGuitar;
    }
}