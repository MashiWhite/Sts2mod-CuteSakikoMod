using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using StringExtensions = BaseLib.Extensions.StringExtensions;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class MeetPerformancePower : CustomPowerModel
{
    public override string CustomPackedIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").PowerImagePath();

    public override string CustomBigIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").PowerImagePath();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠层
    public override bool AllowNegative => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<PressurePower>();
        }
    }

    // 监听压力层数变化，当压力减少时给予格挡
    public override async Task AfterPowerAmountChanged(
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        // 只关心压力能力的减少，且能力拥有者必须是当前玩家
        if (power is not PressurePower || power.Owner != Owner) return;
        if (amount >= 0) return; // 只处理减少（amount为负）

        var reduction = (int)-amount; // 减少的层数（正数）
        if (reduction <= 0) return;

        // 获得格挡 = 减少量 * 本能力层数
        var blockGain = reduction * Amount;
        if (blockGain <= 0) return;

        var blockVar = new BlockVar(blockGain, ValueProp.Move);
        await CreatureCmd.GainBlock(Owner, blockVar, null);
    }
}