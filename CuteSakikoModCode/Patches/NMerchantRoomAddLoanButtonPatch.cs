using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using CuteSakikoMod.CuteSakikoModCode.Nodes;
using MegaCrit.Sts2.Core.Hooks;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

// 在 _Ready 时创建按钮并订阅房间进入事件
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

// 打开库存时显示按钮
[HarmonyPatch(typeof(NMerchantRoom), "OpenInventory")]
public static class NMerchantRoom_OpenInventory_Patch
{
    static void Postfix(NMerchantRoom __instance)
    {
        var button = __instance.GetNodeOrNull<TogawaLoanButton>("TogawaLoanButton");
        button?.ShowButton();
    }
}

// 关闭库存时隐藏按钮
[HarmonyPatch(typeof(NMerchantInventory), "Close")]
public static class NMerchantInventory_Close_Patch
{
    static void Postfix()
    {
        var room = NMerchantRoom.Instance;
        if (room == null) return;
        var button = room.GetNodeOrNull<TogawaLoanButton>("TogawaLoanButton");
        button?.HideButton();
    }
}

// ★ 新增：每次进入房间时，如果是商店房间，重置按钮状态
[HarmonyPatch(typeof(Hook), nameof(Hook.AfterRoomEntered))]
public static class Hook_AfterRoomEntered_Patch
{
    static void Postfix(IRunState runState, AbstractRoom room)
    {
        if (room is not MerchantRoom) return;
        var nMerchantRoom = NMerchantRoom.Instance;
        if (nMerchantRoom == null) return;
        var button = nMerchantRoom.GetNodeOrNull<TogawaLoanButton>("TogawaLoanButton");
        button?.ResetForNewVisit();
    }
}