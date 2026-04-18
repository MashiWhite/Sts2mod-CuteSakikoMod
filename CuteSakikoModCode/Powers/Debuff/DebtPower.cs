using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using StringExtensions = BaseLib.Extensions.StringExtensions;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;

public sealed class DebtPower : CustomPowerModel
{
    public override string CustomPackedIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").PowerImagePath();

    public override string CustomBigIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").PowerImagePath();

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    // 回合结束时造成等量伤害并移除
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;
        if (Amount <= 0) return;

        await CreatureCmd.Damage(choiceContext, Owner, Amount, ValueProp.Unblockable | ValueProp.Unpowered, Owner,
            null);
        await PowerCmd.Remove(this);
    }
}