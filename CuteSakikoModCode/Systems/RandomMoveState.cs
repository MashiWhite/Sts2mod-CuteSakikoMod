using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

/// <summary>
/// 与 MoveState 用法完全一致，区别是 GetNextState 从一组候选 state id 里随机挑下一个意图。
/// rng 由 MonsterMoveStateMachine.RollMove 注入，自动联机同步。
/// </summary>
public class RandomMoveState : MoveState
{
    private readonly string[] _candidates;

    public RandomMoveState(
        string stateId,
        Func<IReadOnlyList<Creature>, Task> onPerform,
        string[] candidates,
        params AbstractIntent[] intents)
        : base(stateId, onPerform, intents)
    {
        if (candidates == null || candidates.Length == 0)
            throw new ArgumentException("candidates must not be empty", nameof(candidates));
        _candidates = candidates;
    }

    public override string GetNextState(Creature owner, Rng rng)
    {
        return _candidates[rng.NextInt(_candidates.Length)];
    }
}