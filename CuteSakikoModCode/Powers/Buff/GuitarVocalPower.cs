using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public class GuitarVocalPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public async Task OnNoteGained(PlayerChoiceContext context, int count)
    {
        var owner = Owner;
        if (owner?.CombatState == null) return;

        int totalDamage = count * Amount;
        if (totalDamage <= 0) return;

        var hittableEnemies = owner.CombatState.HittableEnemies.ToList();
        if (hittableEnemies.Count == 0) return;

        var rng = owner.CombatState.RunState.Rng.CombatTargets;
        var target = rng.NextItem(hittableEnemies);

        Flash();

        await CreatureCmd.Damage(
            context,
            target,
            totalDamage,
            ValueProp.Unpowered,
            Owner,
            null,
            null
        );
    }
}