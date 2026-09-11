using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;

public class CutClothes : CuteRanaCard
{
    public CutClothes() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 动态伤害预览：实时显示当前格挡值
            yield return new CalculationBaseVar(0m);
            yield return new ExtraDamageVar(1m);
            yield return new CalculatedDamageVar(ValueProp.Move)
                .WithMultiplier((card, _) => (decimal)card.Owner.Creature.Block);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        // 1. 记录减半/清除前的格挡值（用于后续伤害）
        int preBlockDamage = Owner.Creature.Block;

        if (IsUpgraded)
        {
            // 升级后：清除所有敌人的全部格挡
            foreach (var enemy in CombatState.Enemies.Where(e => e.IsAlive))
            {
                if (enemy.Block > 0)
                    await CreatureCmd.LoseBlock(choiceContext, enemy, enemy.Block, enemy);
            }
        }
        else
        {
            // 未升级：减少所有人一半格挡（自己、队友、敌人）
            var targetsToLoseBlock = new List<Creature>();
            foreach (var player in CombatState.Players)
                targetsToLoseBlock.Add(player.Creature);
            targetsToLoseBlock.AddRange(CombatState.Enemies);

            foreach (var creature in targetsToLoseBlock)
            {
                int loseAmount = creature.Block / 2;
                if (loseAmount > 0)
                    await CreatureCmd.LoseBlock(choiceContext, creature, loseAmount, creature);
            }
        }

        // 2. 用减半/清除前的格挡值造成伤害（伤害数字在减格挡之后出现）
        if (preBlockDamage > 0)
        {
            await DamageCmd.Attack(preBlockDamage)
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        // 升级效果已在 OnPlay 中通过 IsUpgraded 分支处理
    }
}