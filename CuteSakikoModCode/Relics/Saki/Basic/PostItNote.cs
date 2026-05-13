using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Basic;

public sealed class PostItNote : KabutoNote
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != Owner.Creature.Side) return;

        if (combatState.RoundNumber == 1)
        {
            await PowerCmd.Apply<PressurePower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 5, Owner.Creature, null);
            Flash();
        }

        if (combatState.HittableEnemies != null)
        {
            foreach (var enemy in combatState.HittableEnemies)
                await PowerCmd.Apply<PressurePower>(new ThrowingPlayerChoiceContext(), enemy, 5, Owner.Creature, null);
        }
    }
}