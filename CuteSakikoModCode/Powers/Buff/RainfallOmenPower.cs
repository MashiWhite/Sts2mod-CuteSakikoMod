using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class RainfallOmenPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 层数即格挡值，可叠加

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        // 只在玩家回合结束时触发
        if (side != CombatSide.Player) return;
        if (!participants.Contains(Owner)) return;

        // 检查是否有敌人意图为攻击
        bool anyEnemyIntendsToAttack = Owner.CombatState?.HittableEnemies
            .Any(e => e.Monster != null && e.Monster.IntendsToAttack) ?? false;

        if (anyEnemyIntendsToAttack)
        {
            // 获得的格挡量等于本能力的层数
            int blockAmount = Amount;
            if (blockAmount > 0)
                await CreatureCmd.GainBlock(Owner, blockAmount, ValueProp.Move, null);
        }
    }
}