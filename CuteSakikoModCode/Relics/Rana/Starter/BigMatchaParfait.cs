using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;

public class BigMatchaParfait : MatchaParfait
{
    // 初始杯数由 AfterObtained 决定，不在这里写死
    protected override int GetInitialCharges() => 0;

    public BigMatchaParfait()
    {
        DrawAmount = 2;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(DrawAmount),
        new EnergyVar(EnergyGain)
    };

    public override async Task AfterObtained()
    {
        // 优先继承被替换的普通芭菲的杯数（+6）
        if (PendingTransferCharges.HasValue)
        {
            Charges = PendingTransferCharges.Value + 6;
            PendingTransferCharges = null; // 用完即清，避免污染
        }
        else
        {
            // 玩家原本没有芭菲时，直接给 12 杯
            Charges = 12;
        }

        await Task.CompletedTask;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is RestSiteRoom) Charges += 8; // 休息处增加 8 杯
        return Task.CompletedTask;
    }
}