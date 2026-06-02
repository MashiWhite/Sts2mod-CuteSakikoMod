using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class MonsterMoveHelper
{
    /// <summary>
    /// 安全地获取怪物的“有效”后续动作 ID。
    /// 跳过 STUNNED 等临时状态，避免状态机找不到动作。
    /// 返回 null 时，状态机会自动选取下一个合法动作。
    /// </summary>
    public static string? GetEffectiveFollowUpId(MonsterModel monster)
    {
        var move = monster.NextMove;
        // 最多回溯 5 次
        for (int i = 0; i < 5 && move != null; i++)
        {
            if (move.Id != "STUNNED")
                return move.Id;

            // 如果当前是 STUNNED，取它的 FollowUpStateId
            if (!string.IsNullOrEmpty(move.FollowUpStateId) && move.FollowUpStateId != "STUNNED")
                return move.FollowUpStateId;

            break; // 否则放弃
        }
        return null;
    }
}