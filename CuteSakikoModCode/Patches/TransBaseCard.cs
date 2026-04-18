using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Ancient;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Basic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(ArchaicTooth), "get_TranscendenceUpgrades")]
public static class ArchaicToothPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref Dictionary<ModelId, CardModel> __result)
    {
        var goWorkId = ModelDb.Card<GoWork>().Id;
        var noWorkCard = ModelDb.Card<NoWork>();

        if (!__result.ContainsKey(goWorkId)) __result[goWorkId] = noWorkCard;
    }
}