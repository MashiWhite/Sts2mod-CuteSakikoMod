
using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class CommunicateProperly() : CuteAnonCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                yield return CardKeyword.Exhaust;
            }
        }

        private static string GetFallbackFollowUpStateId(MonsterModel monster)
        {
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
                        return statesDict.ContainsKey("Idle") ? "Idle" : statesDict.Keys.First();
                    }
                }
            }
            return "Idle";
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            var targetCreature = cardPlay.Target;
            if (targetCreature == null || !targetCreature.IsMonster) return;

            var monster = targetCreature.Monster;
            string followUpId = GetFallbackFollowUpStateId(monster);

            var defendIntent = new DefendIntent();
            var customMove = new MoveState(
                "COMMUNICATE_PROPERLY_BLOCK",
                async (targets) =>
                {
                    await CreatureCmd.GainBlock(targetCreature, 15, ValueProp.Move, null);
                },
                defendIntent
            )
            {
                FollowUpStateId = followUpId
            };

            monster.SetMoveImmediate(customMove, forceTransition: true);
        }

        protected override void OnUpgrade()
        {
            AddKeyword(CardKeyword.Retain);
        }
    }
}