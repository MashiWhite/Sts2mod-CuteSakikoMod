
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class CommunicateProperly : CuteAnonCard
    {
        public CommunicateProperly() : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
        {
        }

        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                yield return CardKeyword.Exhaust;
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            var targetCreature = cardPlay.Target;
            if (targetCreature == null || !targetCreature.IsMonster) return;

            var monster = targetCreature.Monster;

            // DefendIntent 无参构造，仅作为图标标记
            var defendIntent = new DefendIntent();

            var customMove = new MoveState(
                "COMMUNICATE_PROPERLY_BLOCK",
                async (targets) =>
                {
                    await CreatureCmd.GainBlock(targetCreature, 15, ValueProp.Move, null);
                },
                defendIntent
            );

            monster.SetMoveImmediate(customMove, forceTransition: true);
        }

        protected override void OnUpgrade()
        {
            AddKeyword(CardKeyword.Retain);
        }
    }
}