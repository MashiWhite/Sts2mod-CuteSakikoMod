using CuteSakikoMod.CuteSakikoModCode.Monsters.Elite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Encounters.Elite;

/// <summary>薇欧拉（第 2 幕精英）。</summary>
[RegisterActEncounter(typeof(Underdocks))]
public class ViolaEncounter : CuteEncounters
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Viola>()
    ];

    public override RoomType RoomType => RoomType.Elite;
    public override bool IsWeak => false;

    public override float GetCameraScaling() => 0.9f;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return
        [
            (ModelDb.Monster<Viola>().ToMutable(), null)
        ];
    }
}
