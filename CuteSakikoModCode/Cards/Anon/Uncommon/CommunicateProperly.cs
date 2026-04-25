
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class CommunicateProperly() : CuteAnonCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get { yield return CardKeyword.Exhaust; }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            var targetCreature = cardPlay.Target;
            // 安全检查：目标必须存在、存活、且为怪物
            if (targetCreature == null || !targetCreature.IsAlive || !targetCreature.IsMonster)
                return;

            var monster = targetCreature.Monster;
            if (monster == null) return;

            var defendIntent = new DefendIntent();
            var customMove = new MoveState(
                "COMMUNICATE_PROPERLY_BLOCK",
                async (targets) =>
                {
                    // 再次检查目标是否存活（异步操作期间可能死亡）
                    if (!targetCreature.IsAlive) return;
                    await CreatureCmd.GainBlock(targetCreature, 15, ValueProp.Move, null);
                },
                defendIntent
            )
            {
                FollowUpStateId = "Idle" // 固定后续状态，避免状态机异常
            };

            // 最终确认怪物未被移除
            if (targetCreature.IsAlive && targetCreature.Monster != null)
                monster.SetMoveImmediate(customMove, forceTransition: true);
        }

        protected override void OnUpgrade()
        {
            AddKeyword(CardKeyword.Retain);
        }
    }
}