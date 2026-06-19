using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public class NaiVitalityPower : CuteSakikoModPower
{
    private bool _willTriggerNextTurn;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // ★ 改为 BeforeSideTurnEnd，在手牌被弃掉前检测
    public override async Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Player) return;

        var combatState = Owner?.Player?.PlayerCombatState;
        if (combatState == null) return;

        var hasEnergy = combatState.Energy > 0;
        var handCount = combatState.Hand?.Cards.Count ?? 0;
        _willTriggerNextTurn = hasEnergy || handCount > 0;

        await Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;

        if (!_willTriggerNextTurn) return;
        _willTriggerNextTurn = false;

        await PlayerCmd.GainEnergy(Amount, player);
        await CardPileCmd.Draw(choiceContext, Amount, player);
    }
}