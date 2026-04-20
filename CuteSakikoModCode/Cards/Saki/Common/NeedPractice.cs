using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using CuteSakikoMod.CuteSakikoModCode.Pools.Saki;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;


public class NeedPractice() : CuteSakikoModCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
{

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(9m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 1. 造成伤害
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 2. 获取目标身上的压力
        var targetPressure = cardPlay.Target.GetPower<PressurePower>();
        if (targetPressure == null || targetPressure.Amount <= 0)
            return;

        // 3. 计算需要分配的压力：一半（向上取整）
        var halfPressure = (targetPressure.Amount + 1) / 2;

        // 4. 获取其他敌人（不包括当前目标）
        var otherEnemies = CombatState.HittableEnemies
            .Where(e => e != cardPlay.Target)
            .ToList();

        // 5. 仅当有其他敌人时才进行压力转移
        if (otherEnemies.Count > 0)
        {
            // 平均分配压力（整数分配，余数依次加给前几个敌人）
            var baseAmount = halfPressure / otherEnemies.Count;
            var remainder = halfPressure % otherEnemies.Count;

            for (var i = 0; i < otherEnemies.Count; i++)
            {
                var enemy = otherEnemies[i];
                var amountToAdd = baseAmount + (i < remainder ? 1 : 0);
                if (amountToAdd <= 0) continue;

                var existing = enemy.GetPower<PressurePower>();
                if (existing != null)
                {
                    await PowerCmd.ModifyAmount(existing, amountToAdd, Owner.Creature, this);
                }
                else
                {
                    var clonedPressure = (PressurePower)targetPressure.ClonePreservingMutability();
                    await PowerCmd.Apply(clonedPressure, enemy, amountToAdd, Owner.Creature, this);
                }
            }

            // 6. 减少目标自身的压力（减去已分配出去的一半）
            await PowerCmd.ModifyAmount(targetPressure, -halfPressure, Owner.Creature, this);
        }
        // 若无其他敌人，则不进行任何压力转移，目标压力保持不变
    }

    protected override void OnUpgrade()
    {
        // 升级：伤害 9 -> 13
        DynamicVars.Damage.UpgradeValueBy(4m);
    }
}