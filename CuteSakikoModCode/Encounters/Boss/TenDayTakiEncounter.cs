using CuteSakikoMod.CuteSakikoModCode.Monsters.Boss.Tenday;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Encounters.Boss;

[RegisterActEncounter(typeof(Glory))]
public class TenDayTakiEncounter : CuteEncounters
{
    public override string BossNodePath => "res://CuteSakikoMod/images/ui/map/TenDayTaki";

    public override EncounterAssetProfile AssetProfile => new(
        EncounterScenePath: "res://CuteSakikoMod/scenes/encounter/tenday_encounter.tscn",
        RunHistoryIconPath: "res://CuteSakikoMod/images/ui/run_history/tenday_taki_encounter.png",
        RunHistoryIconOutlinePath: "res://CuteSakikoMod/images/ui/run_history/tenday_taki_encounter_outline.png"
    );

    public override IReadOnlyList<string> Slots => new[]
    {
        "boss",
        "snail1", "snail2", "snail3",
        "snail4", "snail5", "snail6"
    };

    public override float GetCameraScaling() => 0.8f;

    public override Vector2 GetCameraOffset() => Vector2.Down * 50f + Vector2.Left * 100f;

    public override RoomType RoomType => RoomType.Boss;
    public override bool IsWeak => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters => new MonsterModel[]
    {
        ModelDb.Monster<TenDayTaki>(),
        ModelDb.Monster<TenDayAnon>(),
        ModelDb.Monster<TenDayRana>(),
        ModelDb.Monster<TenDayTomori>(),
        ModelDb.Monster<TenDayUmirin>(),
        ModelDb.Monster<TenDaySoyo>(),
        ModelDb.Monster<TenDaySaki>()
    };

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return new[] { (ModelDb.Monster<TenDayTaki>().ToMutable(), (string?)"boss") };
    }
}