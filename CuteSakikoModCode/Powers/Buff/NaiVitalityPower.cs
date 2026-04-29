using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public class NaiVitalityPower : CuteSakikoModPower
{
    private bool _willTriggerNextTurn;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // 在回合结束之前（手牌和能量尚未重置）检查条件
    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player) return;

        var combatState = Owner?.Player?.PlayerCombatState;
        if (combatState == null) return;

        // OR 逻辑：能量 > 0 或手牌数 > 0
        var hasEnergy = combatState.Energy > 0;
        var handCount = combatState.Hand?.Cards.Count ?? 0;
        _willTriggerNextTurn = hasEnergy || handCount > 0;

        await Task.CompletedTask;
    }

    // 下回合开始时触发
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!_willTriggerNextTurn) return;
        _willTriggerNextTurn = false;

        if (player == null) return;

        await PlayerCmd.GainEnergy(1, player);
        await CardPileCmd.Draw(choiceContext, 1, player);
    }
}