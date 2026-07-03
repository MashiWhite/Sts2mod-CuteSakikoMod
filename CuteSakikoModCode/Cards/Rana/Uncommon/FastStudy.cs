using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.ValueProps;
using System.Linq;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class FastStudy : CuteRanaCard
{
    public FastStudy() : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
    {
    }

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Exhaust; }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new DamageVar(0m, ValueProp.Move); }
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

            AttackIntent firstAttack = null;
            foreach (var intent in move.Intents)
            {
                if (intent is AttackIntent attackIntent && attackIntent.DamageCalc != null)
                {
                    firstAttack = attackIntent;
                    break; // 只取第一个攻击意图
                }
            }

            if (firstAttack != null)
            {
                decimal rawDamage = firstAttack.DamageCalc();
                int repeats = firstAttack.Repeats; // 总攻击次数
                totalDamage += (int)(rawDamage * repeats);
            }
        }

        if (totalDamage > 0)
        {
            DynamicVars.Damage.BaseValue = totalDamage;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .FromCard(this,cardPlay)
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