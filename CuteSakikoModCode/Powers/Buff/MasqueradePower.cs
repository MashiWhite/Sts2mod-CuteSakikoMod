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
    // 保存被移除的能力的原始实例，确保动态变量（如计数器）不丢失
    private readonly List<(Creature creature, PowerModel power, int amount)> _removedPowers = new();

    /// <summary>假面舞会是否正在生效</summary>
    public static bool IsActive { get; private set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldDie(Creature creature)
    {
        return false;
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        IsActive = true;
    }

    public Task RemoveAllPowers(PlayerChoiceContext choiceContext)
    {
        if (Owner.CombatState == null)
            return Task.CompletedTask;

        var allCreatures = Owner.CombatState.Creatures.ToList();

        foreach (var creature in allCreatures)
        {
            if (creature.IsDead) continue;

            // 保存所有需要被移除的能力的原始实例
            foreach (var power in creature.Powers)
            {
                if (power is MasqueradePower || power is SandpitPower)
                    continue;

                _removedPowers.Add((creature, power, power.Amount));
            }

            // 批量移除：只保留 Masquerade 和 Sandpit
            creature.RemoveAllPowersInternalExcept(
                creature.Powers.Where(p => p is MasqueradePower || p is SandpitPower)
            );
        }

        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        var tasks = new List<Task>();
        foreach (var (creature, power, amount) in _removedPowers)
        {
            if (creature.IsDead) continue;

            // 直接复用保存的原始实例，恢复全部内部状态
            tasks.Add(PowerCmd.Apply(choiceContext, power, creature, amount, Owner, null));
        }

        _removedPowers.Clear();

        await Task.WhenAll(tasks);

        await PowerCmd.Remove(this);
        IsActive = false;
    }
}