
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using MegaCrit.Sts2.Core.Models;


namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class CalmnessLadyPower : CuteSakikoModPower
{

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    // 在伤害计算后（Osty 之后），将伤害改为0，实现免疫
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

        // 记录攻击者，用于后续反弹
        _pendingDealer = dealer;
        _pendingDamage = (int)amount;
        return 0m; // 免疫伤害
    }

    public override async Task AfterModifyingHpLostAfterOsty()
    {
        if (Amount <= 0) return;
        // 消耗一层能力
        await PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -1, null, null);
        // 反弹伤害
        if (_pendingDealer != null && _pendingDealer.IsAlive && _pendingDamage > 0)
        {
            await CreatureCmd.Damage(
                new ThrowingPlayerChoiceContext(),
                _pendingDealer,
                _pendingDamage,
                ValueProp.Unpowered,
                Owner,
                null);
        }
        _pendingDealer = null;
        _pendingDamage = 0;
    }

    private Creature? _pendingDealer;
    private int _pendingDamage;
}