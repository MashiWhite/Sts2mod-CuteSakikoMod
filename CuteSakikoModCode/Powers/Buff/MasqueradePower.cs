using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using STS2RitsuLib.Utils;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class MasqueradePower : CuteSakikoModPower
{
    private readonly List<(Creature creature, PowerModel power, int amount)> _removedPowers = new();

    // 使用 ICombatState 作为键类型，兼容 Owner.CombatState 的接口返回类型
    private static readonly AttachedState<ICombatState, bool> _isActive = new(() => false);

    public static bool IsActiveFor(ICombatState? combat) => combat != null && _isActive[combat];

    public static bool IsActive =>
        CombatManager.Instance.DebugOnlyGetState() is { } cs && IsActiveFor(cs);

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // ========== 死亡阻止逻辑 ==========
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

    public override bool ShouldStopCombatFromEnding() => true;

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature.Side == CombatSide.Enemy && creature.CurrentHp < 1)
            await CreatureCmd.SetCurrentHp(creature, 1);
    }
    // =================================================================

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        if (Owner.CombatState is { } cs)
            _isActive[cs] = true;
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (oldOwner.CombatState is { } cs)
            _isActive[cs] = false;
        await base.AfterRemoved(oldOwner);
    }

    /// <summary>
    /// 移除场上所有其他能力（保留 MasqueradePower 和 SandpitPower）。
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

        SortRemovedPowers();
        return Task.CompletedTask;
    }

    private void SortRemovedPowers()
    {
        _removedPowers.Sort(static (a, b) =>
        {
            int c = string.CompareOrdinal(GetCreatureKey(a.creature), GetCreatureKey(b.creature));
            if (c != 0) return c;
            return string.CompareOrdinal(a.power.Id.Entry, b.power.Id.Entry);
        });
    }

    private static string GetCreatureKey(Creature creature)
    {
        if (creature.Player != null)
            return $"P:{creature.Player.NetId}";
        if (creature.Monster != null)
            return $"M:{creature.Monster.Id.Entry}:{creature.SlotName ?? ""}";
        return $"U:{creature.CombatId?.ToString() ?? ""}";
    }

    // 回合开始时：归还所有被移除的能力，然后移除假面舞会（结束）
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        // 顺序恢复，使用 silent: true 跳过动画与等待
        foreach (var (creature, power, amount) in _removedPowers)
        {
            if (creature.IsDead) continue;

            await PowerCmd.Apply(
                choiceContext,
                power,
                creature,
                amount,
                Owner,
                null,
                silent: true
            );
        }

        _removedPowers.Clear();

        // 假面舞会结束
        await PowerCmd.Remove(this);
        if (Owner.CombatState is { } cs)
            _isActive[cs] = false;
    }
}