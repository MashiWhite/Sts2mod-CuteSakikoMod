using CuteSakikoMod.CuteSakikoModCode.Monsters.Boss;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Encounters.Boss;

// 注册到第三幕 Glory（可根据需要改为其他 Act）
[RegisterActEncounter(typeof(Glory))]
public class StarAnonEncounter : ModEncounterTemplate
{
    // 可能出现的怪物列表（这里只有一个）
    public override IEnumerable<MonsterModel> AllPossibleMonsters => [ModelDb.Monster<StarAnon>()];

    // 房间类型为 Boss
    public override RoomType RoomType => RoomType.Boss;

    // 是否属于弱怪池（Boss 通常不是）
    public override bool IsWeak => false;

    public override EncounterAssetProfile AssetProfile => new(
        RunHistoryIconPath: "res://CuteSakikoMod/images/ui/run_history/star_anon_encounter.png",
        RunHistoryIconOutlinePath: "res://CuteSakikoMod/images/ui/run_history/star_anon_encounter_outline.png"
    );


    // 生成怪物：一个星爱音，槽位由系统自动分配
    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        return
        [
            (ModelDb.Monster<StarAnon>().ToMutable(), null)
        ];
    }
}