using CuteSakikoMod.CuteSakikoModCode.Monsters.Elite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Encounters.Elite;

[RegisterActEncounter(typeof(Overgrowth))]
[RegisterActEncounter(typeof(Underdocks))]
public class JenniferEncounter : CuteEncounters
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<Jennifer>()];
    public override RoomType RoomType => RoomType.Elite;
    public override bool IsWeak => false;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return [(ModelDb.Monster<Jennifer>().ToMutable(), null)];
    }
}