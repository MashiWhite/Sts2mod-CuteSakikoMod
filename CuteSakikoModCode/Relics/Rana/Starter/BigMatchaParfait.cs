
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;

public class BigMatchaParfait : MatchaParfait
{
    public BigMatchaParfait()
    {
        DrawAmount = 2;
        // 基类已初始化 6 杯，"获得时增加 6 杯" 即在此基础上 +6
        Charges += 6;
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new CardsVar(DrawAmount),
        new EnergyVar(EnergyGain)
    };

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        if (room is RestSiteRoom) Charges += 8; // 本地化："在休息处获得 8 杯"
        return Task.CompletedTask;
    }
}