using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class MemoryCmd
{
    public static async Task Forget(PlayerChoiceContext choiceContext, IEnumerable<CardModel> cards,
        CardModel? source = null, bool removeFromMemory = true)
    {
        var list = cards.ToList();
        if (list.Count == 0) return;
        var player = list[0].Owner;

        // ★ 在牌堆操作之前触发遗忘事件（确保事件一定触发）
        await MemoryCardPileManager.FireCardsForgotten(choiceContext, list, source);

        // 尝试获取遗忘牌堆类型
        var forgetPileType = ForgetCardPile.GetPileType();
        if (forgetPileType == null)
        {
            Log.Error($"[MemoryCmd] ForgetCardPile.GetPileType() returned null for player {player.NetId}! " +
                      $"Card count: {list.Count}, source: {source?.Id.Entry ?? "null"}");
            return;
        }

        var memoryPile = removeFromMemory ? MemoryCardPile.Get(player) : null;

        foreach (var card in list)
        {
            if (card.Pile?.Type == forgetPileType) continue;

            await CardPileCmd.Add(card, forgetPileType);

            var pressure = player.Creature.GetPower<PressurePower>();
            if (pressure != null)
                await PowerCmd.ModifyAmount(choiceContext, pressure, -2, player.Creature, source);

            if (memoryPile != null && removeFromMemory)
            {
                var toRemove = memoryPile.Cards.Where(c => c.Id == card.Id).ToList();
                foreach (var mCard in toRemove)
                    memoryPile.RemoveInternal(mCard);
            }
        }
    }

    public static List<CardModel> Recall(PlayerChoiceContext choiceContext, Player player, int count, bool upgraded,
        CardModel source = null)
    {
        var memoryPile = MemoryCardPile.Get(player);
        if (memoryPile == null || memoryPile.Cards.Count == 0)
            return new List<CardModel>();
        var rng = player.RunState.Rng.Shuffle;
        var available = memoryPile.Cards.ToList();
        if (count > available.Count) count = available.Count;
        var chosen = new List<CardModel>();
        var tempList = available.ToList();
        for (var i = 0; i < count; i++)
        {
            var index = rng.NextInt(0, tempList.Count);
            chosen.Add(tempList[index]);
            tempList.RemoveAt(index);
        }

        var newCards = new List<CardModel>();
        foreach (var template in chosen)
        {
            var clone = player.RunState.CloneCard(template);
            if (upgraded && !clone.IsUpgraded)
                clone.UpgradeInternal();
            newCards.Add(clone);
        }

        if (newCards.Count > 0)
        {
            var handPile = player.PlayerCombatState?.Hand;
            if (handPile != null)
            {
                foreach (var c in newCards)
                    handPile.AddInternal(c, silent: true);
                handPile.InvokeContentsChanged();
            }
        }

        return newCards;
    }
}