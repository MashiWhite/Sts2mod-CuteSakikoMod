using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;

namespace CuteSakikoMod.CuteSakikoModCode.Patches.EventButton;

[HarmonyPatch(typeof(MerchantEntry), nameof(MerchantEntry.EnoughGold), MethodType.Getter)]
public static class MerchantEntryEnoughGoldTgwCardPatch
{
#if !STS2_AT_LEAST_0_113_0
    // 0.111：MerchantEntry 没有公开 Player，只能反射私有字段
    private static readonly AccessTools.FieldRef<MerchantEntry, Player> PlayerRef =
        AccessTools.FieldRefAccess<MerchantEntry, Player>("_player");
#endif

    private static void Postfix(MerchantEntry __instance, ref bool __result)
    {
        if (!__result) return;

#if STS2_AT_LEAST_0_113_0
        // 0.113 起公开 Player 属性，不再依赖私有字段名
        var player = __instance.Player;
#else
        var player = PlayerRef(__instance);
#endif
        if (player == null) return;

        var relic = player.GetRelic<TgwCard>();
        if (relic == null || !relic.ShopLimitActive) return;

        // 本次商店累计消费 + 当前价格 ≤ 欠款
        if (relic.ShopSpentThisVisit + __instance.Cost > relic.Debt)
            __result = false;
    }
}