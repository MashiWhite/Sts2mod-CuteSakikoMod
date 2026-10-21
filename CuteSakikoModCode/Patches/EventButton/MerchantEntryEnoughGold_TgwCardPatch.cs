using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;

namespace CuteSakikoMod.CuteSakikoModCode.Patches.EventButton;

[HarmonyPatch(typeof(MerchantEntry), nameof(MerchantEntry.EnoughGold), MethodType.Getter)]
public static class MerchantEntryEnoughGoldTgwCardPatch
{
    private static readonly AccessTools.FieldRef<MerchantEntry, Player> PlayerRef =
        AccessTools.FieldRefAccess<MerchantEntry, Player>("_player");

    private static void Postfix(MerchantEntry __instance, ref bool __result)
    {
        if (!__result) return;

        var player = PlayerRef(__instance);
        if (player == null) return;

        var relic = player.GetRelic<TgwCard>();
        if (relic == null || !relic.ShopLimitActive) return;

        // 本次商店累计消费 + 当前价格 ≤ 欠款
        if (relic.ShopSpentThisVisit + __instance.Cost > relic.Debt)
            __result = false;
    }
}