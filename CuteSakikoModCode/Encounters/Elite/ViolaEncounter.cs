using CuteSakikoMod.CuteSakikoModCode.Monsters.Elite;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Encounters.Elite;

/// <summary>薇欧拉（第 2 幕 Hive 精英）。Act Index：Overgrowth/Underdocks=0（第1幕变体）、Hive=1（第2幕）、Glory=2（第3幕）。</summary>
[RegisterActEncounter(typeof(Hive))]
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
