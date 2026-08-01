using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
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
        CardModel? cardSource)  // 补上缺失参数
    {
        if (dealer != Owner) return 0m;
        return -amount * 0.5m;
    }

    // 自身受到的伤害翻倍
    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)  // 补上缺失参数
    {
        if (target != Owner) return 1m;
        if (Amount <= 0) return 1m;
        return 2m;
    }

    // 记录是否受到过伤害（未格挡），跨回合累积
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (result.UnblockedDamage > 0)
            _hasTakenDamageSinceLastOwnTurnEnd = true;
    }

    // 在自己的回合结束时，如果期间受到过伤害，则移除该能力
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        if (_hasTakenDamageSinceLastOwnTurnEnd && Amount > 0)
        {
            await PowerCmd.Remove(this);
            _hasTakenDamageSinceLastOwnTurnEnd = false;
        }
    }
    
}