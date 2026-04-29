using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

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
        if (targetCreature == null || !targetCreature.IsAlive || !targetCreature.IsMonster)
            return;

        var monster = targetCreature.Monster;
        if (monster == null) return;

        var followUpId = MonsterUtils.GetFallbackFollowUpStateId(monster);
        if (string.IsNullOrEmpty(followUpId)) return;

        var defendIntent = new DefendIntent();
        var customMove = new MoveState(
            "COMMUNICATE_PROPERLY_BLOCK",
            async targets =>
            {
                if (!targetCreature.IsAlive) return;
                await CreatureCmd.GainBlock(targetCreature, 15, ValueProp.Move, null);
            },
            defendIntent
        )
        {
            FollowUpStateId = followUpId
        };

        if (targetCreature.IsAlive && targetCreature.Monster != null)
            monster.SetMoveImmediate(customMove, true);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}