using BaseLib.Abstracts;
using BaseLib.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class BlackRebirthPower : CustomPowerModel
{
    private int _cumulativePressure; // 累计未用于回血的压力

    public override string CustomPackedIconPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").PowerImagePath();

    public override string CustomBigIconPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").PowerImagePath();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<PressurePower>();
        }
    }

    // 添加动态变量，用于描述中显示还需多少压力
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new RemainingPressureVar(5 - _cumulativePressure); }
    }

    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (amount <= 0) return;
        if (power is not PressurePower) return;

        var gained = (int)amount;
        _cumulativePressure += gained;

        var healCount = _cumulativePressure / 5;
        if (healCount > 0)
        {
            var totalHeal = healCount * Amount; // Amount 为 BlackRebirthPower 层数（基础1，升级2）
            await CreatureCmd.Heal(Owner, totalHeal);
            _cumulativePressure -= healCount * 5;
        }

        // 更新动态变量值（剩余所需压力）
        if (DynamicVars.TryGetValue("RemainingPressure", out var var)) var.BaseValue = 5 - _cumulativePressure;
    }
}

// 自定义动态变量，表示还需多少压力触发下次回血
public class RemainingPressureVar : DynamicVar
{
    public RemainingPressureVar(decimal baseValue) : base("RemainingPressure", baseValue)
    {
    }
}