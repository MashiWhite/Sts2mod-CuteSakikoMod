
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;


namespace CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Uncommon;

public class RingAnnualPass : CuteRanaRelic
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

    // 在悬停提示中显示抹茶芭菲的预览
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            // FromRelic 返回 IEnumerable<IHoverTip>，需要逐个 yield
            foreach (var tip in HoverTipFactory.FromRelic<MatchaParfait>())
                yield return tip;
        }
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        await base.AfterRoomEntered(room);

        // 检查是否进入商店房间
        if (room is not MerchantRoom) return;
        if (Owner == null) return;

        // 查找玩家身上的抹茶芭菲遗物
        var parfait = Owner.GetRelic<MatchaParfait>();
        if (parfait == null) return;

        // 增加3杯
        MatchaParfait.AddCharges(parfait, 3);
        Flash(); // 闪一下视觉效果
    }
}
