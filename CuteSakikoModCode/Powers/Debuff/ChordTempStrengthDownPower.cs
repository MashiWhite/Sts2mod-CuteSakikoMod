using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;

/// <summary>
///     和弦造成的临时力量下降（本回合）
/// </summary>
[RegisterPower]
public class ChordTempStrengthDownPower : ModTemporaryPowerTemplate
{
    public override LocString Title => new("powers", Id.Entry + ".title");

    public override PowerAssetProfile AssetProfile => this.PowerAssetProfile();

    // 内部实际应用的力量能力
    public override PowerModel InternallyAppliedPower =>
        ModelDb.Power<StrengthPower>().ToMutable();

    // 来源模型（用于悬停提示，可留空）
    public override AbstractModel OriginModel => null;

    // 表示这是一个负面效果（这样 SignedAmount 会自动取负）
    protected override bool IsPositive => false;

    // 持续到本回合结束（即敌人回合结束时移除）
    protected override bool UntilEndOfOtherSideTurn => false;

    // 不额外持续回合
    protected override int LastForXExtraTurns => 1; // 修复 KeyNotFoundException

    // 可选：如果你希望悬停提示额外显示力量相关信息，保留并修正
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            // 注意：FromPower 需要传入 power 实例，而不是泛型参数
            yield return HoverTipFactory.FromPower(InternallyAppliedPower);
        }
    }
}