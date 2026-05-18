using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Combat.Powers;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;

[RegisterPower]
public class ChordTempStrengthDownPower : ModTemporaryPowerTemplate
{
    public override LocString Title => new("powers", Id.Entry + ".title");
    public override PowerAssetProfile AssetProfile => this.PowerAssetProfile();

    public override PowerModel InternallyAppliedPower =>
        ModelDb.Power<StrengthPower>().ToMutable();

    public override AbstractModel OriginModel => null;

    protected override bool IsPositive => false;

    // 持续到本回合结束（即敌人回合结束时移除）
    protected override bool UntilEndOfOtherSideTurn => false;
    protected override int LastForXExtraTurns => 0; // 0 表示不跨回合

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower(InternallyAppliedPower); }
    }
}