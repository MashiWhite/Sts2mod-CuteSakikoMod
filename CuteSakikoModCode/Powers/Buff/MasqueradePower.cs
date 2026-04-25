
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;


namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class MasqueradePower : CuteSakikoModPower
{
    private List<(Creature owner, ModelId powerId, int amount)> _removedPowers = new();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public async Task RemoveAllPowers()
    {
        if (Owner.CombatState == null) return;

        var allCreatures = Owner.CombatState.Creatures.ToList();
        foreach (var creature in allCreatures)
        {
            var powers = creature.Powers.ToList();
            foreach (var power in powers)
            {
                if (power is MasqueradePower) continue;
                _removedPowers.Add((creature, power.Id, power.Amount));
                await PowerCmd.Remove(power);
            }
        }
    }

    // 无限次阻止敌人死亡（每次都会触发）
    public override bool ShouldDie(Creature creature)
    {
        if (!creature.IsPlayer) return false; // 敌人永远不死
        return base.ShouldDie(creature);
    }

    // 阻止死亡后，回复1血并击晕
    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature.IsPlayer) return;
        await CreatureCmd.SetCurrentHp(creature, 1);
        await CreatureCmd.Stun(creature);
        Flash();
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
                await PowerCmd.Apply(choiceContext,newPower, creature, amount, Owner, null);
            }
        }
        _removedPowers.Clear();
        await PowerCmd.Remove(this);
    }
}