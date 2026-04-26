
using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using StringExtensions = BaseLib.Extensions.StringExtensions;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;

/// <summary>
/// 和弦造成的临时力量下降（本回合）
/// </summary>


public class ChordTempStrengthDownPower : CustomTemporaryPowerModel
{
    // 内部实际应用的力量能力（这里是降低 StrengthPower）
    public override PowerModel InternallyAppliedPower => ModelDb.Power<StrengthPower>().ToMutable();
    
    public override string CustomPackedIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").PowerImagePath();

    public override string CustomBigIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").PowerImagePath();

    // 来源模型，用于悬停提示显示来源（可返回 null 或虚拟模型）
    public override AbstractModel OriginModel => null;

    // 应用力量的委托：负值表示减少力量
    protected override Func<PlayerChoiceContext, Creature, Decimal, Creature?, CardModel?, bool, Task> ApplyPowerFunc =>
        async (ctx, target, amount, applier, cardSource, silent) =>
        {
            await PowerCmd.Apply<StrengthPower>(ctx, target, -amount, applier, cardSource, silent);
        };

    // 持续到本回合结束（即轮到敌人回合结束时移除）
    protected override bool UntilEndOfOtherSideTurn => false;

    // 不额外持续回合
    protected override int LastForXExtraTurns => 0;

    // 能力类型：负面（Debuff）
    public override PowerType Type => PowerType.Debuff;

    // 是否为正收益（用于显示颜色等）
    public virtual bool IsPositive => false;



    // 悬停提示
    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<StrengthPower>();
        }
    }
}