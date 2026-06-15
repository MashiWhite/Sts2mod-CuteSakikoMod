using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public class GuitarVocalPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠加

    // 原版风格：每次获得音符立即攻击一个随机敌人
    public async Task OnNoteGained(int count)
    {
        var owner = Owner;
        if (owner?.CombatState == null) return;

        int totalDamage = count * Amount;
        if (totalDamage <= 0) return;

        // 使用 CombatState 的只读列表
        var hittableEnemies = owner.CombatState.HittableEnemies;
        if (hittableEnemies.Count == 0) return;

        // 使用正确的随机数生成器（战斗目标选择）
        var rng = owner.CombatState.RunState.Rng.CombatTargets;
        var target = rng.NextItem(hittableEnemies);

        // 原版能力都会闪烁一下
        Flash();

        // 执行伤害
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            target,
            totalDamage,
            ValueProp.Unpowered,
            Owner,
            null
        );
    }
}