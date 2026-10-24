using CuteSakikoMod.CuteSakikoModCode.Nodes;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using VakuuMoeButton = CuteSakikoMod.CuteSakikoModCode.Nodes.SakiButton.VakuuMoeButton;

namespace CuteSakikoMod.CuteSakikoModCode.Patches.EventButton;

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