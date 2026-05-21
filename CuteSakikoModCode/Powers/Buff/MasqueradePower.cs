using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class MasqueradePower : CuteSakikoModPower
{
    private readonly List<(Creature creature, PowerModel power, int amount)> _removedPowers = new();

    public static bool IsActive { get; private set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // ========== 死亡阻止逻辑（已统一，替代所有旧版伤害修改） ==========
    // 1. 禁止敌人因任何原因死亡（伤害、Doom、处决等）
    public override bool ShouldDie(Creature creature)
    {
        if (creature.Side == CombatSide.Enemy)
            return false;
        return base.ShouldDie(creature);
    }

    public override bool ShouldDieLate(Creature creature)
    {
        if (creature.Side == CombatSide.Enemy)
            return false;
        return base.ShouldDieLate(creature);
    }

    // 2. 阻止战斗提前结束（因为敌人永远不会真正死亡）
    public override bool ShouldStopCombatFromEnding()
    {
        return true;
    }

    // 3. 死亡被阻止时立即锁血到 1 点，同时避免递归崩溃
    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature.Side == CombatSide.Enemy)
        {
            if (creature.CurrentHp < 1)
                await CreatureCmd.SetCurrentHp(creature, 1);
        }
    }
    // =================================================================

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        IsActive = true;
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        IsActive = false;
        await base.AfterRemoved(oldOwner);
    }

    /// <summary>
    /// 移除场上所有其他能力（保留 MasqueradePower 和 SandpitPower）
    /// </summary>
    public Task RemoveAllPowers(PlayerChoiceContext choiceContext)
    {
        if (Owner.CombatState == null)
            return Task.CompletedTask;

        var allCreatures = Owner.CombatState.Creatures.ToList();

        foreach (var creature in allCreatures)
        {
            if (creature.IsDead) continue;

            foreach (var power in creature.Powers)
            {
                if (power is MasqueradePower || power is SandpitPower)
                    continue;

                _removedPowers.Add((creature, power, power.Amount));
            }

            creature.RemoveAllPowersInternalExcept(
                creature.Powers.Where(p => p is MasqueradePower || p is SandpitPower)
            );
        }

        return Task.CompletedTask;
    }

    // 回合开始时：归还所有被移除的能力，然后移除假面舞会（结束）
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        // 归还所有被移除的能力
        var tasks = new List<Task>();
        foreach (var (creature, power, amount) in _removedPowers)
        {
            if (creature.IsDead) continue;
            tasks.Add(PowerCmd.Apply(choiceContext, power, creature, amount, Owner, null));
        }
        _removedPowers.Clear();

        await Task.WhenAll(tasks);

        // 假面舞会结束
        await PowerCmd.Remove(this);
        IsActive = false;
    }
}