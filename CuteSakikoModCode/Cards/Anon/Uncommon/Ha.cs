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

        // 造成伤害
        var damage = DynamicVars.Damage.BaseValue;
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(targetCreature)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 目标必须依然存活且为怪物
        if (!targetCreature.IsAlive || !targetCreature.IsMonster) return;

        var monster = targetCreature.Monster;
        if (monster == null) return;

        // 新增判断：如果怪物当前意图是 DEAD_MOVE，则跳过修改（千足虫死亡状态）
        if (monster.NextMove?.Id == "DEAD_MOVE") return;

        // 安全地获取怪物原本的意图 ID，跳过临时状态（如 STUNNED）
        string? originalMoveId = MonsterMoveHelper.GetEffectiveFollowUpId(monster);

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
                        null
                    );
                }
            },
            attackIntent
        )
        {
            FollowUpStateId = originalMoveId
        };

        // 强制插入自定义动作
        if (targetCreature.IsAlive && targetCreature.Monster != null)
            monster.SetMoveImmediate(customMove, true);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(5m);
    }

  
}