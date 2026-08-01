using MegaCrit.Sts2.Core.Models;
using System.Linq;
using System.Reflection;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class MonsterMoveHelper
{
    /// <summary>
    /// 原始方法保持不变（可能用于其他场景）。
    /// </summary>
    public static string? GetEffectiveFollowUpId(MonsterModel monster)
    {
        var move = monster.NextMove;
        for (int i = 0; i < 5 && move != null; i++)
        {
            if (move.Id != "STUNNED")
                return move.Id;
            if (!string.IsNullOrEmpty(move.FollowUpStateId) && move.FollowUpStateId != "STUNNED")
                return move.FollowUpStateId;
            break;
        }
        return null;
    }

    /// <summary>
    /// 安全获取一个在状态机中已注册的有效后续状态 ID。
    /// 优先使用原始方法的结果，但如果该 ID 未注册或指向自定义状态，则从状态机中选取一个默认状态。
    /// </summary>
    public static string? GetSafeFollowUpId(MonsterModel monster)
    {
        // 尝试原始方法
        string? rawId = GetEffectiveFollowUpId(monster);

        // 获取状态机（通过反射或假定属性）
        var machine = GetMoveStateMachine(monster);
        if (machine != null && rawId != null)
        {
            // 如果原始 ID 已在状态机中注册且不是自定义 ID，则直接使用
            if (machine.States.ContainsKey(rawId) && rawId != "HA_ATTACK")
                return rawId;
        }

        // 否则从状态机中选取一个安全状态（排除 STUNNED 和 HA_ATTACK）
        if (machine != null)
        {
            var states = machine.States.Keys;
            // 首选状态列表（可根据怪物类型调整）
            string[] preferred = { "HEAVY_ATTACK_1", "PERFORM", "MONOLOGUE", "HEAVY_ATTACK_2" };
            foreach (var pref in preferred)
            {
                if (states.Contains(pref))
                    return pref;
            }
            // 返回第一个非 STUNNED 且非 HA_ATTACK 的状态
            return states.FirstOrDefault(id => id != "STUNNED" && id != "HA_ATTACK");
        }

        // 完全失败则返回 null，状态机会自动处理
        return null;
    }

    // 辅助方法：通过反射获取 MonsterMoveStateMachine
    private static MonsterMoveStateMachine? GetMoveStateMachine(MonsterModel monster)
    {
        // 尝试直接访问公共属性
        var prop = monster.GetType().GetProperty("MoveStateMachine");
        if (prop != null && prop.CanRead)
            return prop.GetValue(monster) as MonsterMoveStateMachine;

        // 尝试访问字段
        var field = monster.GetType().GetField("_moveStateMachine", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null)
            return field.GetValue(monster) as MonsterMoveStateMachine;

        return null;
    }
}