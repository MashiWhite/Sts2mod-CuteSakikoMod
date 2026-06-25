using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib;
using System.Linq;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class MemoryCmd
{
    public static async Task Forget(PlayerChoiceContext choiceContext, IEnumerable<CardModel> cards,
        CardModel? source = null, bool removeFromMemory = true)
    {
        var list = cards.ToList();
        if (list.Count == 0) return;
        var player = list[0].Owner;

        // ✅ 确保记忆牌堆已初始化
        await MemoryCardPile.EnsureInitializedAsync(player);

        await MemoryCardPileManager.FireCardsForgotten(choiceContext, list, source);

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
            // 安全检查：仅当卡牌在战斗中且位于某个战斗牌堆时，才执行遗忘操作
            if (card.Pile == null || !card.Pile.Type.IsCombatPile())
            {
                Log.Warn($"[MemoryCmd] Skipping card {card.Id.Entry} because it is not in a combat pile.");
                continue;
            }

            if (card.Pile.Type == forgetPileType) continue;

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

    public static async Task<List<CardModel>> Recall(
    PlayerChoiceContext choiceContext,
    Player player,
    bool allowChoose = false,
    int count = 1,
    bool fillHand = false,
    bool upgraded = false,
    CardModel? source = null)
{
    // 确保记忆牌堆已初始化
    await MemoryCardPile.EnsureInitializedAsync(player);

    var sourceCards = MemoryCardPile.GetCanonicalCards(player);
    if (sourceCards.Count == 0)
        return new List<CardModel>();

    int targetCount = 0;

    if (fillHand)
    {
        var handPile = PileType.Hand.GetPile(player);
        int currentSize = handPile?.Cards.Count ?? 0;
        int maxHandSize = RitsuLibFramework.GetMaxHandSize(player);
        targetCount = Math.Max(0, maxHandSize - currentSize);
        if (targetCount == 0)
            return new List<CardModel>();
    }
    else
    {
        targetCount = Math.Max(1, count);
    }

    if (allowChoose)
    {
        // 生成可选择的战斗卡牌实例（已与 CombatState 关联）
        var selectableCards = sourceCards
            .Select(template => MemoryCardPile.CreateCardFromMemorySnapshot(player, template))
            .Where(c => c != null)
            .Cast<CardModel>()
            .ToList();

        if (selectableCards.Count == 0)
            return new List<CardModel>();

        int maxSelect = Math.Min(targetCount, selectableCards.Count);
        int minSelect = Math.Min(1, maxSelect);

        var prefs = new CardSelectorPrefs(
            new LocString("cards", "CUTE_SAKIKO_MOD_CARD_RECALL.selectionScreenPrompt"),
            minSelect,
            maxSelect
        );

        var selected = await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            selectableCards,
            player,
            prefs
        );

        var selectedList = selected.ToList();

        // 升级处理
        foreach (var card in selectedList)
        {
            if (upgraded && !card.IsUpgraded)
            {
                card.UpgradeInternal();
                card.FinalizeUpgradeInternal();
            }
        }

        // ✅ 使用 AddGeneratedCardToCombat 加入手牌
        if (selectedList.Count > 0)
        {
            await CardPileCmd.AddGeneratedCardsToCombat(selectedList, PileType.Hand, player);
        }

        return selectedList;
    }
    else // allowChoose == false
    {
        var rng = player.RunState.Rng.Shuffle;
        var sourceList = sourceCards.ToList();
        if (sourceList.Count == 0)
            return new List<CardModel>();

        var newCards = new List<CardModel>();
        int toAdd = targetCount;

        if (fillHand)
        {
            // 填满手牌：允许重复选择同一模板
            for (int i = 0; i < toAdd; i++)
            {
                var template = sourceList[rng.NextInt(0, sourceList.Count)];
                var newCard = MemoryCardPile.CreateCardFromMemorySnapshot(player, template);
                if (newCard != null)
                {
                    if (upgraded && !newCard.IsUpgraded)
                    {
                        newCard.UpgradeInternal();
                        newCard.FinalizeUpgradeInternal();
                    }
                    newCards.Add(newCard);
                }
            }
        }
        else
        {
            // 普通回忆：每个模板只能选一次
            toAdd = Math.Min(toAdd, sourceList.Count);
            var tempList = sourceList.ToList();
            for (int i = 0; i < toAdd; i++)
            {
                int index = rng.NextInt(0, tempList.Count);
                var template = tempList[index];
                tempList.RemoveAt(index);
                var newCard = MemoryCardPile.CreateCardFromMemorySnapshot(player, template);
                if (newCard != null)
                {
                    if (upgraded && !newCard.IsUpgraded)
                    {
                        newCard.UpgradeInternal();
                        newCard.FinalizeUpgradeInternal();
                    }
                    newCards.Add(newCard);
                }
            }
        }

        if (newCards.Count > 0)
        {
            await CardPileCmd.AddGeneratedCardsToCombat(newCards, PileType.Hand, player);
        }
        return newCards;
    }
}
}