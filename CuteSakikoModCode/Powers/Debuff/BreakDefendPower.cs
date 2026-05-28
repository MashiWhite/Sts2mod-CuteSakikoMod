using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;

public sealed class BreakDefendPower : CuteSakikoModPower
{
    private int _pendingHits;
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    /// <summary>
    ///     每次受到攻击伤害时，直接流失 2 点生命（无视格挡/无形/任何减伤）
    /// </summary>
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || Amount <= 0) return;

        _pendingHits++;

        // 直接扣减生命值，不触发额外伤害/动画
        var newHp = Math.Max(0, Owner.CurrentHp - 2);
        await CreatureCmd.SetCurrentHp(Owner, newHp);
    }

    /// <summary>
    ///     攻击结束后一次性扣除所有命中对应的层数
    /// </summary>
    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (_pendingHits <= 0) return;

        var hits = _pendingHits;
        _pendingHits = 0;

        if (Amount <= 0) return;

        await PowerCmd.ModifyAmount(choiceContext, this, -hits, command.Attacker, null);

        if (Amount <= 0)
            await PowerCmd.Remove(this);
    }
}