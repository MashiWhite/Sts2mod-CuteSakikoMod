
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class DeliciousPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (creature != Owner) return;

        int healAmount = Amount;
        if (healAmount <= 0) return;

        var combatState = Owner.CombatState;
        if (combatState == null) return;

        // 为场上所有存活生物回复层数等量生命值（包括所有敌人和玩家）
        var allCreatures = combatState.Creatures.ToList();
        foreach (var c in allCreatures)
        {
            if (c.IsDead) continue;
            await CreatureCmd.Heal(c, healAmount);
        }
    }
}