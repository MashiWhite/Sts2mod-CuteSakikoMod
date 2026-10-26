
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event;

public class GradeFirstNotebook : CuteSakikoEventRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;

        var smith = options.OfType<SmithRestSiteOption>().FirstOrDefault();
        if (smith == null) return false;

        // 获取当前牌库中可升级的牌数
        int upgradableCount = player.Deck.UpgradableCardCount;

        // 如果可升级的牌数 <= 当前的敲牌次数，说明没有多余的牌可以敲了
        // 此时直接返回 false，避免原版选牌界面因为要求选 2 张但实际只有 1 张而卡死
        if (upgradableCount <= smith.SmithCount) return false;

        // 增加敲牌次数，上限为当前实际可升级的牌数
        smith.SmithCount = Math.Min(smith.SmithCount + 1, upgradableCount);
        return true;
    }
}