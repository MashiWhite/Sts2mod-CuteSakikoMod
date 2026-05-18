using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Basic;

public sealed class PostItNote : KabutoNote // 继承 KabutoNote
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        // 先执行基类的基本逻辑（给玩家施加3层压力，但我们可以不调用基类，或者直接覆盖）
        // 这里我们完全重写，按 PostItNote 自己的规则
        if (side != Owner.Creature.Side) return;

        if (combatState.RoundNumber == 1)
        {
            await PowerCmd.Apply<PressurePower>(new ThrowingPlayerChoiceContext(), Owner.Creature, 5, Owner.Creature,
                null);
            Flash();
        }

        if (combatState.HittableEnemies != null)
            foreach (var enemy in combatState.HittableEnemies)
                await PowerCmd.Apply<PressurePower>(new ThrowingPlayerChoiceContext(), enemy, 5, Owner.Creature, null);
    }
}