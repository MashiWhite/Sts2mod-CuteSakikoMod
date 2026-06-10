using CuteSakikoMod.CuteSakikoModCode.Monsters.Boss;
using Godot;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Content.Patches;

namespace CuteSakikoMod.CuteSakikoModCode.Encounters.Boss;

// 注册到第三幕 Glory（可根据需要改为其他 Act）
[RegisterActEncounter(typeof(Glory))]
public class GreyAnonEncounter : CuteEncounters
{
    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<GreyAnon>()];
    public override RoomType RoomType => RoomType.Boss;
    public override bool IsWeak => false;

    public override string BossNodePath => "res://CuteSakikoMod/images/ui/map/GreyAnon";
    
    public override EncounterAssetProfile AssetProfile => new(
        RunHistoryIconPath: "res://CuteSakikoMod/images/ui/run_history/grey_anon_encounter.png",
        RunHistoryIconOutlinePath: "res://CuteSakikoMod/images/ui/run_history/grey_anon_encounter_outline.png"
    );

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return [(ModelDb.Monster<GreyAnon>().ToMutable(), null)];
    }
}