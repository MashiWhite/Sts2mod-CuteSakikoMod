using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Scaffolding.Content;
using System.Collections.Generic;
using System.Linq;

namespace CuteSakikoMod.CuteSakikoModCode.Encounters.Event;

public class Act1DoubleBossEncounter : ModEncounterTemplate
{
    private static IReadOnlyList<EncounterModel> GetAllAct1BossEncounters()
    {
        if (ModelDb.ActsByIndex == null || ModelDb.ActsByIndex.Count == 0)
            return new List<EncounterModel>();

        return ModelDb.ActsByIndex[0]
            .SelectMany(act => act.AllBossEncounters)
            .Distinct()
            .ToList();
    }

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        GetAllAct1BossEncounters()
            .SelectMany(encounter => encounter.AllPossibleMonsters)
            .Distinct();

    public override RoomType RoomType => RoomType.Elite;
    public override bool IsWeak => false;

    // ★ 槽位名称必须和场景文件中的 Marker2D 节点名完全一致
    public override IReadOnlyList<string> Slots => new[] { "first", "first2" };

    // 指定自定义遭遇场景路径
    public override EncounterAssetProfile AssetProfile => new(
        EncounterScenePath: "res://CuteSakikoMod/scenes/encounter/act1_double_boss.tscn"
    );

    public override float GetCameraScaling() => 1.0f;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        var allBossEncounters = GetAllAct1BossEncounters();

        var selectedEncounters = allBossEncounters
            .OrderBy(_ => Rng.NextFloat())
            .Take(2)
            .ToList();

        var monsters = new List<(MonsterModel, string?)>();
        int index = 0;
        foreach (var encounter in selectedEncounters)
        {
            var boss = encounter.AllPossibleMonsters.First().ToMutable();
            string slot = Slots[index % Slots.Count];
            monsters.Add((boss, slot));
            index++;
        }

        return monsters;
    }
}