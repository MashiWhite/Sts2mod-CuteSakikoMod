using CuteSakikoMod.CuteSakikoModCode.Nodes;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Basic;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Relics;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(NRelicInventoryHolder), nameof(NRelicInventoryHolder._Ready))]
public static class RelicRightClickPatch
{
    private static void Postfix(NRelicInventoryHolder __instance)
    {
        // 原有：AnonGuitar 的处理
        if (__instance.Relic.Model is AnonGuitar anonGuitar)
        {
            __instance.MouseFilter = Control.MouseFilterEnum.Stop;
            __instance.GuiInput += e =>
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

        // 新增：KabutoNote 的处理
        if (__instance.Relic.Model is KabutoNote kabuto)
        {
            __instance.MouseFilter = Control.MouseFilterEnum.Stop;
            __instance.GuiInput += e =>
            {
                if (e is InputEventMouseButton mb
                    && mb.ButtonIndex == MouseButton.Right
                    && mb.Pressed)
                {
                    kabuto.OpenMemoryLibrary();
                    __instance.AcceptEvent();
                }
            };
        }

        // 新增：PostItNote 的处理
        if (__instance.Relic.Model is PostItNote postIt)
        {
            __instance.MouseFilter = Control.MouseFilterEnum.Stop;
            __instance.GuiInput += e =>
            {
                if (e is InputEventMouseButton mb
                    && mb.ButtonIndex == MouseButton.Right
                    && mb.Pressed)
                {
                    postIt.OpenMemoryLibrary();
                    __instance.AcceptEvent();
                }
            };
        }
    }
}