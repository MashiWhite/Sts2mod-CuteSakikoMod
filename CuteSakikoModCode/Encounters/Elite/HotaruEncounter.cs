using CuteSakikoMod.CuteSakikoModCode.Monsters.Elite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Encounters.Elite;

[RegisterActEncounter(typeof(MegaCrit.Sts2.Core.Models.Acts.Hive))]
public class HotaruEncounter : CuteEncounters
{

    public override RoomType RoomType => RoomType.Elite;
    public override bool IsWeak => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Hotaru>()
    ];

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return [ (ModelDb.Monster<Hotaru>().ToMutable(), null) ];
    }
}