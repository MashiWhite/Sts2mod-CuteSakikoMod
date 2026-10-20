using CuteSakikoMod.CuteSakikoModCode.Monsters.Weak;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Encounters.Weak;

[RegisterActEncounter(typeof(Overgrowth))]
[RegisterActEncounter(typeof(Underdocks))]
public class AnonWangEncounter : CuteEncounters
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<AnonWang>()];
    public override RoomType RoomType => RoomType.Monster;
    public override bool IsWeak => true;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return [(ModelDb.Monster<AnonWang>().ToMutable(), null)];
    }
}