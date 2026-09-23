using CuteSakikoMod.CuteSakikoModCode.Relics.Event;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;

public sealed class BreakDownPower : CuteSakikoModPower
{
    private bool _hasTakenDamageSinceLastOwnTurnEnd;

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;

    // 自身造成伤害减少 50%
    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (dealer != Owner) return 0m;

        // 遗物效果：拥有 MasqueradeRhapsody 时，不再降低自身伤害
        if (Owner.Player != null && Owner.Player.Relics.OfType<MasqueradeRhapsody>().Any())
            return 0m;

        return -amount * 0.5m;
    }

    // 自身受到的伤害翻倍
    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (target != Owner) return 1m;
        if (Amount <= 0) return 1m;
        return 2m;
    }

    // 记录是否受到过伤害（未格挡）
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (result.UnblockedDamage <= 0) return;

        // 玩家崩溃：受到伤害时立即移除
        if (Owner.IsPlayer)
        {
            if (Amount > 0)
                await PowerCmd.Remove(this);
            return;
        }

        // 敌人崩溃：标记，待自身回合结束时移除
        _hasTakenDamageSinceLastOwnTurnEnd = true;
    }

    // 敌人崩溃：在其回合结束时若期间受到过伤害，则移除
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (Owner.IsPlayer) return;
        if (side != Owner.Side) return;
        if (_hasTakenDamageSinceLastOwnTurnEnd && Amount > 0)
        {
            await PowerCmd.Remove(this);
            _hasTakenDamageSinceLastOwnTurnEnd = false;
        }
    }
}