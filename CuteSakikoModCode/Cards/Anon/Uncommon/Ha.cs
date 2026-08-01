using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

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

        var damage = DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(targetCreature)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        if (!targetCreature.IsAlive || !targetCreature.IsMonster) return;

        var monster = targetCreature.Monster;
        if (monster == null) return;

        // 使用安全方法获取后续状态 ID
        string? safeFollowUpId = MonsterMoveHelper.GetSafeFollowUpId(monster);
        if (safeFollowUpId == null)
            return; // 无法获取有效后续状态，放弃强制设置

        var attackIntent = new SingleAttackIntent(15);
        var customMove = new MoveState(
            "HA_ATTACK",
            async targets =>
            {
                var players = Owner.Creature.CombatState?.Players.Select(p => p.Creature).ToList();
                if (players != null && players.Any())
                {
                    await CreatureCmd.Damage(
                        choiceContext,
                        players,
                        new DamageVar(15, ValueProp.Move),
                        targetCreature,
                        null,
                        null
                    );
                }
                // 注意：不再需要手动恢复状态，因为 FollowUpStateId 已指向正确状态
            },
            attackIntent
        )
        {
            FollowUpStateId = safeFollowUpId  // 使用安全 ID
        };

        if (targetCreature.IsAlive && targetCreature.Monster != null)
            monster.SetMoveImmediate(customMove, true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }

  
}