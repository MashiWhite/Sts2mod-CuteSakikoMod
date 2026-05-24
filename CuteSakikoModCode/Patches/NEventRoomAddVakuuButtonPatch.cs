using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using CuteSakikoMod.CuteSakikoModCode.Nodes;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(NEventRoom), "_Ready")]
public static class NEventRoomAddVakuuButtonPatch
{
    static void Postfix(NEventRoom __instance)
    {
        if (__instance.HasNode("VakuuMoeButton")) return;

        var button = new VakuuMoeButton();
        button.Name = "VakuuMoeButton";
        __instance.AddChild(button);
    }
}