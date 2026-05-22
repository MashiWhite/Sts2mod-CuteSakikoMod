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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public sealed class OnTurnEndInHandManager : SingletonModel
{
    public OnTurnEndInHandManager()
    {
        ModHelper.SubscribeForCombatStateHooks(Id.Entry, _ => [this]);
    }

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player) return;

        var combatState = CombatManager.Instance?.DebugOnlyGetState();
        if (combatState == null) return;

        foreach (var player in combatState.Players)
        {
            var handPile = player.Piles.FirstOrDefault(p => p.Type == PileType.Hand);
            if (handPile == null) continue;

            var cardsToRemove = new List<CardModel>();
            var handCards = handPile.Cards.ToList();

            foreach (var card in handCards)
            {
                // 只处理 Noise 牌，NotNeeded 已迁移至官方 OnTurnEndInHand
                if (card is Noise noise)
                {
                    var amount = noise.DynamicVars["PressurePower"].IntValue;
                    await PowerCmd.Apply<PressurePower>(choiceContext, player.Creature, amount, player.Creature, noise);
                    cardsToRemove.Add(noise);
                }
            }

            if (cardsToRemove.Count > 0)
                await CardPileCmd.RemoveFromCombat(cardsToRemove);
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await Task.CompletedTask;
    }
}