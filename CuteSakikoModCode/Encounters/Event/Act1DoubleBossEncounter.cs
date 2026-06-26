using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Scaffolding.Content;
using System.Collections.Generic;
using System.Linq;

namespace CuteSakikoMod.CuteSakikoModCode.Encounters.Event;

public class Act1DoubleBossEncounter : ModEncounterTemplate
{
    public override bool IsValidForAct(ActModel act)
    {
        return false; // 只通过事件触发，不自然生成
    }

    private static IReadOnlyList<EncounterModel> GetAllAct1BossEncounters()
    {
        if (ModelDb.ActsByIndex == null || ModelDb.ActsByIndex.Count == 0)
            return new List<EncounterModel>();

        return ModelDb.ActsByIndex[0]
            .SelectMany(act => act.AllBossEncounters)
            .Distinct()
            .ToList();
    }

    /// <summary>
    /// 判断怪物是否为爪牙（拥有 MinionPower 的怪物）
    /// 目前第一幕唯一的爪牙是 KinFollower，如有新增可在此添加
    /// </summary>
    private static bool IsMinionMonster(MonsterModel monster)
    {
        // KinFollower 是爪牙
        if (monster is KinFollower)
            return true;

        // 未来如有其他爪牙，可继续添加：
        // if (monster is SomeOtherMinion) return true;

        return false;
    }

    public override IEnumerable<MonsterModel> AllPossibleMonsters =>
        GetAllAct1BossEncounters()
            .SelectMany(encounter => encounter.AllPossibleMonsters)
            .Where(m => !IsMinionMonster(m))
            .Distinct();

    public override RoomType RoomType => RoomType.Elite;
    public override bool IsWeak => false;

    // 槽位名称必须和场景文件中的 Marker2D 节点名完全一致
    public override IReadOnlyList<string> Slots => new[] { "first", "first2" };

    // 指定自定义遭遇场景路径
    public override EncounterAssetProfile AssetProfile => new(
        EncounterScenePath: "res://CuteSakikoMod/scenes/encounter/act1_double_boss.tscn"
    );

    public override float GetCameraScaling() => 1.0f;

    protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
    {
        // 获取所有非爪牙的 Boss 怪物
        var allBosses = GetAllAct1BossEncounters()
            .SelectMany(encounter => encounter.AllPossibleMonsters)
            .Where(m => !IsMinionMonster(m))
            .Distinct()
            .ToList();

        // 随机选择 2 个不同的 Boss
        var selectedBosses = allBosses
            .OrderBy(_ => Rng.NextFloat())
            .Take(2)
            .ToList();

        var monsters = new List<(MonsterModel, string?)>();
        for (int i = 0; i < selectedBosses.Count; i++)
        {
            var boss = selectedBosses[i].ToMutable();
            string slot = Slots[i % Slots.Count];
            monsters.Add((boss, slot));
        }

        return monsters;
    }
}