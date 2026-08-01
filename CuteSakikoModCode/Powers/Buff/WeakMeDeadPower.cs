using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards; // 新增，用于 CardPlay
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class WeakMeDeadPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 对拥有崩溃的敌人造成的伤害翻倍
    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (dealer == Owner && target != null && target.GetPower<BreakDownPower>() != null) return 2m;
        return 1m;
    }

    // 回合结束时移除该能力
    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        await PowerCmd.Remove(this);
    }
}