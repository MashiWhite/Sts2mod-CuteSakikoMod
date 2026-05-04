using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public sealed class NotNeededManager : SingletonModel
{
    public NotNeededManager()
    {
        ModHelper.SubscribeForCombatStateHooks(Id.Entry, _ => [this]);
    }

    public override bool ShouldReceiveCombatHooks => true;

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player) return;

        var combatState = CombatManager.Instance?.DebugOnlyGetState();
        if (combatState == null) return;

        // 遍历所有玩家（支持多人）
        foreach (var player in combatState.Players)
        {
            var handPile = player.Piles.FirstOrDefault(p => p.Type == PileType.Hand);
            if (handPile == null) continue;

            var notNeededCards = handPile.Cards.OfType<NotNeeded>().ToList();
            foreach (var card in notNeededCards)
            {
                var blockAmount = card.DynamicVars.Block.IntValue;
                await CreatureCmd.GainBlock(player.Creature, blockAmount, ValueProp.Move, null);

                var copy = card.CreateClone();
                copy.GiveSingleTurnRetain();
                await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, player);

                card.GiveSingleTurnRetain();
            }
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        await Task.CompletedTask;
    }
}