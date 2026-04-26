
using System.Reflection;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace CuteSakikoMod.CuteSakikoModCode.Systems
{
    public static class MonsterUtils
    {
        /// <summary>
        /// 获取一个存在的后续状态ID，优先返回 "Idle"，否则返回第一个注册的状态。
        /// 若无法获取任何状态，返回 null。
        /// </summary>
        public static string GetFallbackFollowUpStateId(MonsterModel monster)
        {
            var stateMachineProp = monster.GetType().GetProperty("StateMachine", BindingFlags.Public | BindingFlags.Instance)
                                   ?? monster.GetType().GetProperty("MoveStateMachine", BindingFlags.Public | BindingFlags.Instance);
            if (stateMachineProp == null) return null;
            var stateMachine = stateMachineProp.GetValue(monster);
            if (stateMachine == null) return null;
            var statesProp = stateMachine.GetType().GetProperty("States", BindingFlags.Public | BindingFlags.Instance);
            if (statesProp?.GetValue(stateMachine) is Dictionary<string, MonsterState> statesDict && statesDict.Count > 0)
            {
                return statesDict.ContainsKey("Idle") ? "Idle" : statesDict.Keys.First();
            }
            return null;
        }
    }
}