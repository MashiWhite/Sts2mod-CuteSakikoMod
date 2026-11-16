using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Enchantments;

[RegisterEnchantment]
public sealed class MyGoEnchantment : ModEnchantmentTemplate
{
    private static readonly Lazy<PileType?> _forgetPileType = new(() =>
    {
        try { return ForgetCardPile.GetPileType(); }
        catch { return null; }
    });

    public override bool ShowAmount => false;
    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(
        "CuteSakikoMod/images/enchantments/mygo.png"
    );

    public override bool CanEnchant(CardModel card) => true;

    // 弃牌行为结束后：每张附魔牌各自随机移动到一个牌堆
    public override async Task AfterFlush(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyCollection<CardModel> flushedCards,
        IReadOnlyCollection<CardModel> retainedCards)
    {
        if (player != Card.Owner) return;

        // 卡必须在战斗堆里（不在战斗堆说明已离场）
        var currentPile = Card.Pile;
        if (currentPile == null || !currentPile.IsCombatPile) return;

        var combatState = Card.CombatState;
        if (combatState == null) return;

        var players = combatState.Players.ToList();
        if (players.Count == 0) return;

        // 候选牌堆（不含 None / Deck / Play）
        var pileTypes = new List<PileType>
        {
            PileType.Hand,
            PileType.Draw,
            PileType.Discard,
            PileType.Exhaust,
        };
        if (_forgetPileType.Value is { } forget)
            pileTypes.Add(forget);

        // 构造候选目标（排除"同玩家同牌堆"，避免原地移动）
        var targets = new List<(Player Player, PileType Pile)>();
        foreach (var p in players)
        foreach (var pt in pileTypes)
        {
            if (p == Card.Owner && pt == currentPile.Type) continue;
            targets.Add((p, pt));
        }
        if (targets.Count == 0) return;

        var rng = Card.Owner.RunState.Rng.CombatCardGeneration;
        var chosen = targets[rng.NextInt(targets.Count)];

        if (chosen.Player == Card.Owner)
            await CardPileCmd.Add(Card, chosen.Pile);
        else
        {
#if CSK_GAME_111_OR_NEWER
            await CardPileCmd.GiveToAnotherPlayer(Card, chosen.Player, chosen.Pile);
#else
            // 0.107 没有跨玩家给牌 API：复制一张（保留升级/附魔）交给目标玩家，再移除原卡。
            // ★ 必须用 CombatState.CreateCard：RunState.CreateCard 只登记 RunState，
            //   卡片不在 CombatState 里，CardPileCmd.Add 会抛 "must be added to a CombatState"，
            //   整个 flush 钩子失败 → 联机回合卡死（B-35）。
            var canonical = ModelDb.GetById<CardModel>(Card.Id);
            var targetCombatState = chosen.Player.Creature.CombatState;
            if (targetCombatState == null) return;
            var newCard = targetCombatState.CreateCard(canonical, chosen.Player);

            var upgradeCount = Math.Min(Card.CurrentUpgradeLevel, newCard.MaxUpgradeLevel);
            for (var i = 0; i < upgradeCount; i++)
                CardCmd.Upgrade(newCard);

            if (Card.Enchantment != null)
            {
                var ench = (EnchantmentModel)Card.Enchantment.MutableClone();
                if (ench.CanEnchant(newCard))
                    CardCmd.Enchant(ench, newCard, ench.Amount);
            }

            var targetPile = chosen.Pile.GetPile(chosen.Player);
            await CardPileCmd.Add(newCard, targetPile);
            targetPile.InvokeCardAddFinished();

            await CardPileCmd.RemoveFromCombat(Card);
#endif
        }
    }
}