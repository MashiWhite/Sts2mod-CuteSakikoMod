using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(CombatManager), "FlushPlayerHand")]
public static class FlushPlayerHand_RetainPatch
{
    public static void Prefix(Player player)
    {
        if (player == null) return;
        var discard = PileType.Discard.GetPile(player);
        if (discard == null) return;
        var hand = PileType.Hand.GetPile(player);
        if (hand == null) return;

        var cardsToRescue = discard.Cards
            .Where(c => c.HasTurnEndInHandEffect && c.Keywords.Contains(CardKeyword.Retain))
            .ToList();

        if (cardsToRescue.Count == 0) return;

        var playerHand = NPlayerHand.Instance;
        if (playerHand == null) return;

        foreach (var card in cardsToRescue)
        {
            // 从弃牌堆移除（静默）
            discard.RemoveInternal(card, silent: true);

            // 创建一个新的 NCard 视图
            var ncard = NCard.Create(card);
            if (ncard == null) continue;

            // 添加到手牌数据层
            hand.AddInternal(card, silent: true);

            // 将 NCard 挂载到 UI，产生可见卡牌节点
            playerHand.Add(ncard);
        }
    }
}