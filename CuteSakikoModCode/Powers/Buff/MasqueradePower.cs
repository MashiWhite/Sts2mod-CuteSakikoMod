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
    private readonly List<(Creature creature, ModelId powerId, int amount)> _removedPowers = new();

    /// <summary>假面舞会是否正在生效</summary>
    public static bool IsActive { get; private set; }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldDie(Creature creature) => false;

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

            // 记录所有非假面、非沙坑的能力
            foreach (var power in creature.Powers)
            {
                if (power is MasqueradePower || power is SandpitPower)
                    continue;

                _removedPowers.Add((creature, power.Id, power.Amount));
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

        // 并行恢复所有被移除的能力 —— 大幅加快
        var tasks = new List<Task>();
        foreach (var (creature, powerId, amount) in _removedPowers)
        {
            if (creature.IsDead) continue;

            var canonical = ModelDb.GetById<PowerModel>(powerId);
            if (canonical == null) continue;

            var newPower = canonical.ToMutable();
            tasks.Add(PowerCmd.Apply(choiceContext, newPower, creature, amount, Owner, null));
        }
        _removedPowers.Clear();

        await Task.WhenAll(tasks);   // 并行执行，无依赖能力可安全并行

        await PowerCmd.Remove(this);
        IsActive = false;
    }
}