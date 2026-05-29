using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;

public class BigMatchaParfait : MatchaParfait
{
    public BigMatchaParfait()
    {
        DrawAmount = 2; // 覆盖基类默认值
    }

    public override RelicRarity Rarity => RelicRarity.Starter;

    // 覆盖动态变量以匹配新默认值
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(DrawAmount),
        new EnergyVar(EnergyGain),
    };

    // 休息处恢复 6 点（通过属性赋值，自动刷新UI）
    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is RestSiteRoom) Charges += 6;
        return Task.CompletedTask;
    }

    // ========== 先古升级数据继承 ==========
    private static readonly Dictionary<Player, int> PendingUpgradeCharges = new();

    public override async Task AfterRemoved()
    {
        if (Owner != null) PendingUpgradeCharges[Owner] = Charges;
        await base.AfterRemoved();
    }

    public override async Task AfterObtained()
    {
        if (Owner != null && PendingUpgradeCharges.TryGetValue(Owner, out int oldCharges))
        {
            Charges = oldCharges + 6; // 属性赋值，自动刷新UI
            PendingUpgradeCharges.Remove(Owner);
        }
        else Charges = 6 + 6;
        await base.AfterObtained();
    }
}