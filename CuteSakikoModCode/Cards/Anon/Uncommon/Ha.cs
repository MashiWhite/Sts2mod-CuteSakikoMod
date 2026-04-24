
using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class Ha() : CuteAnonCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new DamageVar(10m, ValueProp.Move);
            }
        }

        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                yield return CardKeyword.Exhaust;
            }
        }

        // 辅助方法：从怪物身上获取一个有效的后续状态 ID
        private static string GetFallbackFollowUpStateId(MonsterModel monster)
        {
            // 尝试通过反射获取状态机
            var stateMachineProp = monster.GetType().GetProperty("StateMachine", BindingFlags.Public | BindingFlags.Instance)
                                ?? monster.GetType().GetProperty("MoveStateMachine", BindingFlags.Public | BindingFlags.Instance);
            if (stateMachineProp != null)
            {
                var stateMachine = stateMachineProp.GetValue(monster);
                if (stateMachine != null)
                {
                    var statesProp = stateMachine.GetType().GetProperty("States", BindingFlags.Public | BindingFlags.Instance);
                    if (statesProp?.GetValue(stateMachine) is Dictionary<string, MonsterState> statesDict && statesDict.Count > 0)
                    {
                        // 优先使用 "Idle"，否则取第一个状态
                        return statesDict.ContainsKey("Idle") ? "Idle" : statesDict.Keys.First();
                    }
                }
            }
            // 最终回退
            return "Idle";
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            var targetCreature = cardPlay.Target;
            if (targetCreature == null || !targetCreature.IsMonster) return;

            // 1. 造成卡牌自身伤害
            var damage = DynamicVars.Damage.BaseValue;
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(targetCreature)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            // 2. 替换敌人意图为造成15点伤害
            var monster = targetCreature.Monster;
            string followUpId = GetFallbackFollowUpStateId(monster);

            var attackIntent = new SingleAttackIntent(15);
            var customMove = new MoveState(
                "HA_ATTACK",
                async (targets) =>
                {
                    var player = Owner.Creature;
                    await CreatureCmd.Damage(choiceContext, player, 15, ValueProp.Move, targetCreature, null);
                },
                attackIntent
            )
            {
                FollowUpStateId = followUpId
            };

            monster.SetMoveImmediate(customMove, forceTransition: true);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(5m);
        }
    }
}