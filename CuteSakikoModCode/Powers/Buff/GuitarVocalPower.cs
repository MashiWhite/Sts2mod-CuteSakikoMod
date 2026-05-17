using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public class GuitarVocalPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠加

    public async Task OnNoteGained(int count)
    {
        var owner = Owner;
        if (owner?.CombatState == null) return;
        var enemies = owner.CombatState.Enemies.Where(e => e.IsHittable).ToList();
        if (enemies.Count == 0) return;

        var damagePerLayer = Amount; // 每层伤害
        var rng = owner.CombatState.RunState.Rng.CombatCardSelection;
        for (var i = 0; i < count; i++)
        {
            var target = rng.NextItem(enemies);
            // 最后一个参数修正为 null
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(), target,
                damagePerLayer, ValueProp.Unpowered, Owner, null);
        }
    }
}