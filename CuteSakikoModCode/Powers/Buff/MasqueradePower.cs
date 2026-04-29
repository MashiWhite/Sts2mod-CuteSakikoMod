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
    private readonly List<(Creature owner, ModelId powerId, int amount)> _removedPowers = new();

    /// <summary>假面舞会是否正在生效（用于补丁跳过沙坑效果）</summary>
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
        IsActive = true; // 假面舞会生效
    }

    public async Task RemoveAllPowers(PlayerChoiceContext choiceContext)
    {
        if (Owner.CombatState == null) return;

        var allCreatures = Owner.CombatState.Creatures.ToList();
        foreach (var creature in allCreatures)
        {
            if (creature.IsDead) continue;

            var powers = creature.Powers.ToList();
            foreach (var power in powers)
            {
                if (power == null || power is MasqueradePower) continue;

                // 完全跳过沙坑能力，不记录也不移除
                if (power is SandpitPower)
                    continue;

                _removedPowers.Add((creature, power.Id, power.Amount));
                await PowerCmd.Remove(power);
            }
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        foreach (var (creature, powerId, amount) in _removedPowers)
        {
            if (creature.IsDead) continue;
            var canonical = ModelDb.GetById<PowerModel>(powerId);
            if (canonical != null)
            {
                var newPower = canonical.ToMutable();
                await PowerCmd.Apply(choiceContext, newPower, creature, amount, Owner, null);
            }
        }

        _removedPowers.Clear();
        await PowerCmd.Remove(this);
        IsActive = false; // 假面舞会结束
    }
}