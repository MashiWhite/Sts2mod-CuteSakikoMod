
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class QuarrelSelfPower : CuteSakikoModPower
{

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override async Task AfterSideTurnStart(CombatSide side, CombatState combatState)
    {
        if (side == Owner.Side)
        {
            var pressure = Owner.GetPower<PressurePower>();
            if (pressure != null)
                // 安全减少压力层数
                await PowerCmd.ModifyAmount(pressure, -Amount, Owner, null);
            await PowerCmd.Remove(this);
        }
    }
}