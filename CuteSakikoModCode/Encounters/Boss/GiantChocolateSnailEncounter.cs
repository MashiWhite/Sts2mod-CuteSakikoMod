using MegaCrit.Sts2.Core.Models.Dread;
using CuteSakikoMod.CuteSakikoModCode.Dominion.Dominions;
using CuteSakikoMod.CuteSakikoModCode.Monsters.Boss.ChocolateSnail;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Encounters.Boss;

[RegisterActEncounter(typeof(Overgrowth))]
[RegisterActEncounter(typeof(Underdocks))]
public class GiantChocolateSnailEncounter : CuteEncounters
{
    /// <summary>恐惧模式：本幕统御（dominion）。</summary>
    public override DominionModel? GetDominion() => ModelDb.Dominion<ChocolateSnailDominion>();

    public override bool HasScene => true;

    public override string BossNodePath => "res://CuteSakikoMod/images/ui/map/ChocolateSnail";

    public override EncounterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/encounter/giant_chocolate_snail_encounter.tscn",
        RunHistoryIconPath:
        "res://CuteSakikoMod/images/ui/run_history/cute_sakiko_mod_encounter_chocolate_snail_encounter.png",
        RunHistoryIconOutlinePath:
        "res://CuteSakikoMod/images/ui/run_history/cute_sakiko_mod_encounter_chocolate_snail_encounter_outline.png"
    );

    public override IReadOnlyList<string> Slots =>
    [
        "boss",
        "snail1", "snail2", "snail3",
        "snail4", "snail5"
    ];

    public override RoomType RoomType => RoomType.Boss;
    public override bool IsWeak => false;

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
    [
        ModelDb.Monster<GiantChocolateSnail>(),
        ModelDb.Monster<SmallChocolateSnail>()
    ];

    public override float GetCameraScaling()
    {
        return 0.8f;
    }

    public override Vector2 GetCameraOffset()
    {
        return Vector2.Down * 50f + Vector2.Left * 100f;
    }

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return
        [
            (ModelDb.Monster<GiantChocolateSnail>().ToMutable(), "boss")
        ];
    }
}