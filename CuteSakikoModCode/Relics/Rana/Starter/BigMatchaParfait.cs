using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;

public class BigMatchaParfait : MatchaParfait
{
    // 重写初始杯数方法，返回 12（基类 6 + 额外 6）
    protected override int GetInitialCharges() => 12;

    public BigMatchaParfait()
    {
        DrawAmount = 2;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(DrawAmount),
        new EnergyVar(EnergyGain)
    };

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is RestSiteRoom) Charges += 8; // 休息处增加 8 杯
        return Task.CompletedTask;
    }
}