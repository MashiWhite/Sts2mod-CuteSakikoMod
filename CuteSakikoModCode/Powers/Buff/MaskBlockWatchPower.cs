using CuteSakikoMod.CuteSakikoModCode.Compat;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

/// <summary>
///     戴上面具：下回合开始、清格挡之前检查。
///     若此时格挡已经耗尽，层数等量伤害；否则安全摘下面具。
///     挂在玩家身上，持续到下一次玩家回合开始。
/// </summary>
public sealed class MaskBlockWatchPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override async Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Player) return;
        if (Owner == null || Owner.IsDead) return;
        if (!participants.Contains(Owner)) return;

        var amount = Amount;
        var blockDepleted = Owner.Block <= 0;

        // 先移除面具，再结算伤害；此钩子发生在清格挡之前。
        await PowerCmd.Remove(this);
        if (!blockDepleted || amount <= 0) return;

        await DamageCompat.Damage(
            choiceContext,
            Owner,
            amount,
            ValueProp.Unblockable | ValueProp.Unpowered,
            null,
            null);
    }
}
