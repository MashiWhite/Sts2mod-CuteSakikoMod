using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Rooms;


namespace CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;

public class BigMatchaParfait : MatchaParfait
{
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
        if (room is RestSiteRoom) Charges += 6;
        return Task.CompletedTask;
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        // 如果是新获得的（Charges 还是默认 6），则设为 12
        if (Charges == 6)
            Charges = 12;
    }
}