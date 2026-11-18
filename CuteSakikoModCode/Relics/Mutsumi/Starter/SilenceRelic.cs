using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Mutsumi.Starter;

/// <summary>
///     缄默（黄瓜睦初始遗物）：每回合最多一次，若本回合没有打出任何攻击牌，
///     在己方回合结束时获得 3 层「言刃」。
/// </summary>
public class SilenceRelic : CuteMutsumiRelic
{
    private bool _playedAttackThisTurn;

    public override RelicRarity Rarity => RelicRarity.Starter;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<WordBladePower>()];

    public override Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (Owner == null) return Task.CompletedTask;
        if (command.Attacker == Owner.Creature && command.ModelSource is CardModel)
            _playedAttackThisTurn = true;
        return Task.CompletedTask;
    }

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Owner == null) return;
        if (side != Owner.Creature.Side) return;

        var shouldGrant = !_playedAttackThisTurn;
        _playedAttackThisTurn = false;

        if (!shouldGrant) return;
        if (!participants.Contains(Owner.Creature)) return;

        await PowerCmd.Apply<WordBladePower>(choiceContext, Owner.Creature, 3m, Owner.Creature, null);
        Flash();
    }
}
