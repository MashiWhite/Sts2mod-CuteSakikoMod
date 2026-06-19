using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;
using System.Reflection;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class FastStudy : CuteRanaCard
{
    public FastStudy() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            yield return CardKeyword.Exhaust;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(0m, ValueProp.Move);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var combatState = Owner.Creature.CombatState;
        if (combatState == null) return;

        var enemies = combatState.Enemies.Where(e => e.IsAlive).ToList();
        if (enemies.Count == 0) return;

        int totalDamage = 0;

        foreach (var enemy in enemies)
        {
            var move = enemy.Monster?.NextMove;
            if (move == null) continue;

            foreach (var intent in move.Intents)
            {
                if (intent is AttackIntent attackIntent)
                {
                    // 通过反射获取 DamageCalc 字段并调用，计算伤害
                    var damageField = typeof(AttackIntent).GetField("DamageCalc",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    if (damageField?.GetValue(attackIntent) is Func<decimal> damageCalc)
                    {
                        totalDamage += (int)damageCalc();
                    }
                    else
                    {
                        // 降级方案：使用 GetTotalDamage 方法
                        totalDamage += attackIntent.GetTotalDamage(new[] { enemy }, enemy);
                    }
                }
            }
        }

        if (totalDamage > 0)
        {
            // 设置伤害变量为计算出的总伤害
            DynamicVars.Damage.BaseValue = totalDamage;

            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this)
                .TargetingAllOpponents(combatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}