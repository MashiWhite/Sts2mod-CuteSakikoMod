using CuteSakikoMod.CuteSakikoModCode.Nodes;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(NEventRoom), "_Ready")]
public static class NEventRoomAddVakuuButtonPatch
{
    private static void Postfix(NEventRoom __instance)
    {
        if (__instance.HasNode("VakuuMoeButton")) return;

        var button = new VakuuMoeButton();
        button.Name = "VakuuMoeButton";
        __instance.AddChild(button);
    }
}