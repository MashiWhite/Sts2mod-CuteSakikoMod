using CuteSakikoMod.CuteSakikoModCode.Monsters.Boss;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Encounters.Boss;

/// <summary>藤都子的 Boss 遭遇：藤都子 + 手偶丸君。</summary>
[RegisterActEncounter(typeof(Hive))]
public class MiyakoEncounter : CuteEncounters
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<Miyako>(),
        ModelDb.Monster<MaruKun>()
    ];

    public override RoomType RoomType => RoomType.Boss;
    public override bool IsWeak => false;

    public override string BossNodePath => "res://CuteSakikoMod/images/ui/map/Miyako";

    public override EncounterAssetProfile AssetProfile => new(
        EncounterScenePath: "res://CuteSakikoMod/scenes/encounter/miyako_encounter.tscn", 
        RunHistoryIconPath: "res://CuteSakikoMod/images/ui/run_history/miyako_encounter.png",
        RunHistoryIconOutlinePath: "res://CuteSakikoMod/images/ui/run_history/miyako_encounter_outline.png"
    );

    public override IReadOnlyList<string> Slots => ["boss", "maru"];

    public override float GetCameraScaling() => 1.0f;

    public override Vector2 GetCameraOffset() => Vector2.Down * 20f + Vector2.Left * 40f;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return
        [
            (ModelDb.Monster<MaruKun>().ToMutable(), "maru"),
            (ModelDb.Monster<Miyako>().ToMutable(), "boss")
        ];
    }
}
