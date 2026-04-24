
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;


namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class OtherHalfPower : CuteSakikoModPower
{
    [SavedProperty] public Creature? Target { get; set; } // 存储目标敌人

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<PressurePower>();
        }
    }

    // 当压力层数变化时触发
    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is not PressurePower || power.Owner != Owner) return;
        if (amount <= 0) return; // 只处理增加
        if (Target == null || Target.IsDead) return;

        var gain = (int)amount;
        if (gain <= 0) return;

        await PowerCmd.Apply<PressurePower>(Target, gain, Owner, cardSource);
    }

    // 当主能力被移除时，同时移除目标身上的标记能力
    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (Target != null && !Target.IsDead)
        {
            var marker = Target.GetPower<OtherHalfTargetPower>();
            if (marker != null)
                await PowerCmd.Remove(marker);
        }
    }
}