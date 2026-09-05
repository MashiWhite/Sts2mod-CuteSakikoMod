
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event;

public class FlaxenHairedGirl : CuteSakikoEventRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        if (Owner == null) return;

        // 获取所有其他队友
        var teammates = Owner.RunState.Players.Where(p => p != Owner).ToList();
        if (teammates.Count == 0) return;

        // 收集所有队友牌组中的卡牌
        var allCards = new List<CardModel>();
        foreach (var teammate in teammates)
        {
            var deck = PileType.Deck.GetPile(teammate);
            allCards.AddRange(deck.Cards);
        }

        // 去重：相同 ID + 升级状态 + 附魔（ID及数量）视为相同
        var distinctCards = allCards
            .GroupBy(c => new
            {
                Id = c.Id,
                UpgradeLevel = c.CurrentUpgradeLevel,
                EnchantmentId = c.Enchantment?.Id,
                EnchantmentAmount = c.Enchantment?.Amount
            })
            .Select(g => g.First())
            .ToList();

        if (distinctCards.Count == 0) return;

        // 为每个去重卡牌创建预览用可变卡牌（保留升级与附魔）
        var previewCards = distinctCards.Select(original =>
        {
            var preview = ModelDb.GetById<CardModel>(original.Id).ToMutable();
            // 复制升级
            for (int i = 0; i < original.CurrentUpgradeLevel; i++)
            {
                preview.UpgradeInternal();
                preview.FinalizeUpgradeInternal();
            }
            // 复制附魔
            if (original.Enchantment != null)
            {
                var ench = (EnchantmentModel)original.Enchantment.MutableClone();
                if (ench.CanEnchant(preview))
                {
                    preview.EnchantInternal(ench, ench.Amount);
                }
            }
            return preview;
        }).ToList();

        // 选择提示
        var prompt = new LocString("relics", "FLAXEN_HAIRED_GIRL.selectionPrompt");
        var prefs = new CardSelectorPrefs(prompt, 1, 1)
        {
            Cancelable = false
        };

        // 手动选择一张
        var context =new BlockingPlayerChoiceContext(); 
        var selected = await CardSelectCmd.FromSimpleGrid(context, previewCards, Owner, prefs);

        if (!selected.Any()) return;

        var chosenPreview = selected.First();

        // 创建真正属于自己的卡牌（复制升级与附魔）
        var newCard = Owner.RunState.CreateCard(ModelDb.GetById<CardModel>(chosenPreview.Id), Owner);
        for (int i = 0; i < chosenPreview.CurrentUpgradeLevel; i++)
        {
            CardCmd.Upgrade(newCard);
        }
        if (chosenPreview.Enchantment != null)
        {
            var ench = (EnchantmentModel)chosenPreview.Enchantment.MutableClone();
            if (ench.CanEnchant(newCard))
            {
                CardCmd.Enchant(ench, newCard, ench.Amount);
            }
        }

        // 加入牌组
        await CardPileCmd.Add(newCard, PileType.Deck);
    }
}