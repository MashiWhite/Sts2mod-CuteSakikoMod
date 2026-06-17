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
    /// <summary>
    ///     将卡牌遗忘（移入 ForgetCardPile 或消耗堆），并消耗压力，然后触发事件。
    ///     所有操作均使用官方同步命令，确保联机状态一致。
    /// </summary>
    public static async Task Forget(PlayerChoiceContext choiceContext, IEnumerable<CardModel> cards,
        CardModel? source = null, bool removeFromMemory = true)
    {
        var list = cards.ToList();
        if (list.Count == 0) return;
        var player = list[0].Owner;

        // 获取遗忘牌堆类型（不依赖实例）
        var forgetPileType = ForgetCardPile.GetPileType(); // 需要在 ForgetCardPile 中添加此方法返回 PileType
        if (forgetPileType == null)
        {
            Log.Error("[MemoryCmd] Cannot get ForgetCardPile type");
            return;
        }

        var memoryPile = removeFromMemory ? MemoryCardPile.Get(player) : null;

        foreach (var card in list)
        {
            if (card.Pile?.Type == forgetPileType) continue;

            // 官方命令：移动卡牌到遗忘牌堆（自动同步）
            await CardPileCmd.Add(card, forgetPileType);

            // 消耗压力（使用官方命令）
            var pressure = player.Creature.GetPower<PressurePower>();
            if (pressure != null)
                await PowerCmd.ModifyAmount(choiceContext, pressure, -2, player.Creature, source);

            // 从记忆牌堆中移除（仅当 memoryPile 可用时）
            if (memoryPile != null && removeFromMemory)
            {
                var toRemove = memoryPile.Cards.Where(c => c.Id == card.Id).ToList();
                foreach (var mCard in toRemove)
                    memoryPile.RemoveInternal(mCard);
            }
        }

        // ★ 关键修复：无论是否获取到 forgetPile 实例，都触发遗忘事件
        await MemoryCardPileManager.FireOnForgottenCards(choiceContext, list, source);
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