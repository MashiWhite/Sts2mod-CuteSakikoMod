using CuteSakikoMod.CuteSakikoModCode.Nodes;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

// 在 _Ready 时创建按钮
[HarmonyPatch(typeof(NMerchantRoom), "_Ready")]
public static class NMerchantRoomAddTogawaButtonPatch
{
    static void Postfix(NMerchantRoom __instance)
    {
        if (__instance.HasNode("TogawaLoanButton")) return;

        var button = new TogawaLoanButton();
        button.Name = "TogawaLoanButton";
        __instance.AddChild(button);
    }
}

// 在 OpenInventory 时显示按钮
[HarmonyPatch(typeof(NMerchantRoom), "OpenInventory")]
public static class NMerchantRoom_OpenInventory_Patch
{
    static void Postfix(NMerchantRoom __instance)
    {
        var button = __instance.GetNodeOrNull<TogawaLoanButton>("TogawaLoanButton");
        button?.ShowButton();
    }
}

// 在 NMerchantInventory.Close 时隐藏按钮
[HarmonyPatch(typeof(NMerchantInventory), "Close")]
public static class NMerchantInventory_Close_Patch
{
    static void Postfix(NMerchantRoom __instance)
    {
        var room = NMerchantRoom.Instance;
        if (room == null) return;
        var button = room.GetNodeOrNull<TogawaLoanButton>("TogawaLoanButton");
        button?.HideButton();
    }
}