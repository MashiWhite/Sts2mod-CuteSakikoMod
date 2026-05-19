using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
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

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new BlockVar(15m, ValueProp.Move); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var targetCreature = cardPlay.Target;
        if (targetCreature == null || !targetCreature.IsAlive || !targetCreature.IsMonster)
            return;

        var monster = targetCreature.Monster;
        if (monster == null) return;

        // ★ 保存怪物原本的意图ID
        var originalMoveId = monster.NextMove.Id;

        var defendIntent = new DefendIntent();
        var customMove = new MoveState(
            "COMMUNICATE_PROPERLY_BLOCK",
            async targets =>
            {
                var block = DynamicVars.Block.IntValue;
                if (!targetCreature.IsAlive) return;
                await CreatureCmd.GainBlock(targetCreature, block, ValueProp.Move, cardPlay);
            },
            defendIntent
        )
        {
            // 执行完自定义动作后，回到怪物原本的下一步动作
            FollowUpStateId = originalMoveId
        };

        if (targetCreature.IsAlive && targetCreature.Monster != null)
            monster.SetMoveImmediate(customMove, true);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}