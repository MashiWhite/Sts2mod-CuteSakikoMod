using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class CalmnessLadyPower : CuteSakikoModPower
{
    private int _pendingDamage;
    private Creature? _pendingDealer;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 在伤害计算后（Osty 之后），将伤害改为0，实现免疫，并记录攻击者
    public override decimal ModifyHpLostAfterOstyLate(
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return amount;
        if (Amount <= 0) return amount;
        if (!props.IsPoweredAttack()) return amount;

        // 记录攻击者与伤害，供后续反弹使用
        _pendingDealer = dealer;
        _pendingDamage = (int)amount;
        return 0m; // 免疫伤害
    }

    // 在伤害实际生效后（包括被修改后），执行反弹逻辑
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (Amount <= 0) return;
        if (!props.IsPoweredAttack()) return;
        if (_pendingDamage <= 0 || _pendingDealer == null || !_pendingDealer.IsAlive) return;

        // 消耗一层能力
        await PowerCmd.ModifyAmount(choiceContext, this, -1, null, null);

        // 反弹伤害
        await CreatureCmd.Damage(
            choiceContext,
            _pendingDealer,
            new DamageVar(_pendingDamage, ValueProp.Unpowered),
            Owner,   // 伤害来源为 Owner（你自己）
            null             // 没有 CardPlay
        );

        // 清理记录
        _pendingDealer = null;
        _pendingDamage = 0;
    }
}