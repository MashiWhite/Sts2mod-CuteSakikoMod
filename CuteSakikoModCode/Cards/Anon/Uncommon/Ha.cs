
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class Ha() : CuteAnonCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get { yield return new DamageVar(10m, ValueProp.Move); }
        }

        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get { yield return CardKeyword.Exhaust; }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            var targetCreature = cardPlay.Target;
            if (targetCreature == null || !targetCreature.IsAlive) return;

            // 1. 造成卡牌自身伤害
            var damage = DynamicVars.Damage.BaseValue;
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(targetCreature)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            // 2. 目标必须在伤害后仍然存活且为怪物，才替换意图
            if (!targetCreature.IsAlive || !targetCreature.IsMonster) return;

            var monster = targetCreature.Monster;
            if (monster == null) return;

            // 使用固定后续状态 "Idle" 避免复杂的状态机反射
            var attackIntent = new SingleAttackIntent(15);
            var customMove = new MoveState(
                "HA_ATTACK",
                async (targets) =>
                {
                    var player = Owner.Creature;
                    // 对手方目标设为玩家本身（或所有玩家），此处简单起见选择第一个玩家
                    var targetsToHit = Owner.Creature.CombatState?.Players.Select(p => p.Creature).ToList();
                    if (targetsToHit != null && targetsToHit.Any())
                        await CreatureCmd.Damage(choiceContext, targetsToHit, 15, ValueProp.Move, targetCreature, null);
                },
                attackIntent
            )
            {
                FollowUpStateId = "Idle"
            };

            // 再次确认怪物未被移除
            if (targetCreature.IsAlive && targetCreature.Monster != null)
                targetCreature.Monster.SetMoveImmediate(customMove, forceTransition: true);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(5m); // 10 → 15
        }
    }
}