using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

[RegisterPower]
public class ChordEndOfTurnExhaustPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        // 只在拥有者所在的阵营回合结束时触发
        if (side != Owner.Side) return;
        if (Amount <= 0) return;

        var player = Owner.Player;
        if (player == null) return;

        // 收集手牌、抽牌堆、弃牌堆中所有牌
        var handCards = PileType.Hand.GetPile(player)?.Cards ?? Enumerable.Empty<CardModel>();
        var drawCards = PileType.Draw.GetPile(player)?.Cards ?? Enumerable.Empty<CardModel>();
        var discardCards = PileType.Discard.GetPile(player)?.Cards ?? Enumerable.Empty<CardModel>();

        var allCards = handCards
            .Concat(drawCards)
            .Concat(discardCards)
            .ToList();

        if (allCards.Count == 0)
        {
            // 无牌可选，直接移除自身
            await PowerCmd.Remove(this);
            return;
        }

        // 层数决定最大可选牌数，可取消（minCount=0）
        var prefs = new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, minCount: 0, maxCount: Amount);
        var selected = await CardSelectCmd.FromSimpleGrid(choiceContext, allCards, player, prefs);

        foreach (var card in selected)
            await CardCmd.Exhaust(choiceContext, card);

        // 无论是否选择，使用后移除自身
        await PowerCmd.Remove(this);
    }
}