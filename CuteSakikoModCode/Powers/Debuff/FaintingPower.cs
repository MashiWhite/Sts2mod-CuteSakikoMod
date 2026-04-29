using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;

public sealed class FaintingPower : CuteSakikoModPower
{
    private int _thresholdHp;


    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;

    // 定义动态变量，用于智能描述
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new IntVar("Threshold", 0); }
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        _thresholdHp = Owner.CurrentHp;
        // 更新动态变量值为触发阈值（获得时生命值的一半）
        if (DynamicVars.TryGetValue("Threshold", out var var)) var.BaseValue = _thresholdHp / 2;
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner) return;
        if (Owner.CurrentHp <= _thresholdHp / 2 && !Owner.IsStunned)
        {
            await CreatureCmd.Stun(Owner);
            await PowerCmd.Remove(this);
        }
    }
}