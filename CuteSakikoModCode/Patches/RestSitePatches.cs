using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.Nodes;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(NRestSiteRoom), "_Ready")]
public static class NRestSiteRoom_AddPracticeGuitarButton_Patch
{
    private static void Postfix(NRestSiteRoom __instance)
    {
        if (__instance.HasNode("PracticeGuitarButton")) return;

        var state = RunManager.Instance.DebugOnlyGetState();
        if (state == null) return;

        var player = LocalContext.GetMe(state.Players);
        if (player == null) return;

        var guitar = player.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) return;

        var button = new PracticeGuitarButton();
        button.Name = "PracticeGuitarButton";
        __instance.AddChild(button);
    }
}