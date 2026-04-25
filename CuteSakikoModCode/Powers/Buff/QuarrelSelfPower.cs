
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class QuarrelSelfPower : CuteSakikoModPower
{

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side == Owner.Side)
        {
            var pressure = Owner.GetPower<PressurePower>();
            if (pressure != null)
                // 使用 ThrowingPlayerChoiceContext 补足缺少的 ctx 参数
                await PowerCmd.ModifyAmount(
                    new ThrowingPlayerChoiceContext(), pressure, -Amount, Owner, null);
            await PowerCmd.Remove(this);
        }
    }
}