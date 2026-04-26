using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Relics;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Nodes;

namespace CuteSakikoMod.CuteSakikoModCode.Patches
{
    [HarmonyPatch(typeof(NRelicInventoryHolder), nameof(NRelicInventoryHolder._Ready))]
    public static class RelicRightClickPatch
    {
        static void Postfix(NRelicInventoryHolder __instance)
        {
            if (__instance.Relic.Model is AnonGuitar)
            {
                __instance.MouseFilter = Control.MouseFilterEnum.Stop;
                __instance.GuiInput += (InputEvent e) =>
                {
                    if (e is InputEventMouseButton mb
                        && mb.ButtonIndex == MouseButton.Right
                        && mb.Pressed)
                    {
                        ChordLibraryScreen.OpenBrowse();
                        __instance.AcceptEvent();
                    }
                };
            }
        }
    }
}