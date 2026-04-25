using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;   // 新增，提供 PlayerChoiceContext
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class MeetPerformancePower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
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
    // 签名增加 PlayerChoiceContext 参数
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,      // 新增
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is not PressurePower || power.Owner != Owner) return;
        if (amount >= 0) return;

        var reduction = (int)-amount;
        if (reduction <= 0) return;

        var blockGain = reduction * Amount;
        if (blockGain <= 0) return;

        var blockVar = new BlockVar(blockGain, ValueProp.Move);
        await CreatureCmd.GainBlock( Owner, blockVar, null);
    }
}