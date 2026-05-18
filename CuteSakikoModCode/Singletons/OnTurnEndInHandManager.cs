using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Status;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public sealed class OnTurnEndInHandManager : SingletonModel
{
    public OnTurnEndInHandManager()
    {
        ModHelper.SubscribeForCombatStateHooks(Id.Entry, _ => [this]);
    }

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player) return;

        var combatState = CombatManager.Instance?.DebugOnlyGetState();
        if (combatState == null) return;

        foreach (var player in combatState.Players)
        {
            var handPile = player.Piles.FirstOrDefault(p => p.Type == PileType.Hand);
            if (handPile == null) continue;

            // 收集需要移除的牌（如 Noise）
            var cardsToRemove = new List<CardModel>();

            // 复制一份手牌列表以防修改
            var handCards = handPile.Cards.ToList();
            foreach (var card in handCards)
                // 跳过卡牌自己的 OnTurnEndInHand 标记，由我们统一处理
                // if (card.HasTurnEndInHandEffect) continue; // 如果某些牌仍想自助，可保留
                switch (card)
                {
                    case NotNeeded notNeeded:
                    {
                        var blockAmount = notNeeded.DynamicVars.Block.IntValue;
                        await CreatureCmd.GainBlock(player.Creature, blockAmount, ValueProp.Move, null);

                        var copy = notNeeded.CreateClone();
                        copy.GiveSingleTurnRetain();
                        await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, player);

                        notNeeded.GiveSingleTurnRetain();
                        break;
                    }
                    case Noise noise:
                    {
                        var amount = noise.DynamicVars["PressurePower"].IntValue;
                        await PowerCmd.Apply<PressurePower>(choiceContext, player.Creature, amount, player.Creature,
                            noise);
                        cardsToRemove.Add(noise);
                        break;
                    }
                    // 未来可以在此扩展其他卡牌
                }

            // 批量移除
            if (cardsToRemove.Count > 0)
                await CardPileCmd.RemoveFromCombat(cardsToRemove);
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await Task.CompletedTask;
    }
}