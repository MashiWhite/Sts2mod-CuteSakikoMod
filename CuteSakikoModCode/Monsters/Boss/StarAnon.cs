using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Curse;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Boss;

[RegisterMonster]
public class StarAnon : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 420, 400);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 470, 450);

    private MoveState _deadState;
    private bool _reviveUsed;

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }

    public override MonsterAssetProfile AssetProfile => new(
        VisualsScenePath: "res://CuteSakikoMod/scenes/monster/star_anon_boss.tscn"
    );

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // 开局仅添加 RetrogradePower（复活逻辑将由该能力调用怪物的 TriggerDeadState）
        await PowerCmd.Apply<RetrogradePower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        await PowerCmd.Apply<TimeWatchPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // ★ 从外部触发复活状态的入口，由 RetrogradePower 调用
    public async Task TriggerDeadState()
    {
        // 强制切换到复活状态，执行复活动作
        SetMoveImmediate(_deadState, true);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // 复活动作：仅执行一次，与实验体一致
        _deadState = new MoveState("RESPAWN_MOVE", RespawnMove,
            new AbstractIntent[] { new HealIntent(), new BuffIntent() })
        {
            MustPerformOnceBeforeTransitioning = true
        };

        // 普通动作
        var buffStr = new MoveState("BUFF_STRENGTH", BuffStrengthMove, new BuffIntent());
        var attack2 = new MoveState("DOUBLE_ATTACK", DoubleAttackMove,
            new MultiAttackIntent(10, 2), new StatusIntent(10));
        var heavyAttack = new MoveState("HEAVY_ATTACK", HeavyAttackMove,
            new SingleAttackIntent(30));
        var buffStr2 = new MoveState("BUFF_STRENGTH2", BuffStrengthMove2, new BuffIntent());

        buffStr.FollowUpState = attack2;
        attack2.FollowUpState = heavyAttack;
        heavyAttack.FollowUpState = buffStr2;
        buffStr2.FollowUpState = attack2;

        // 复活后回到 buffStr
        _deadState.FollowUpState = buffStr;

        return new MonsterMoveStateMachine(
            new List<MonsterState> { _deadState, buffStr, attack2, heavyAttack, buffStr2 },
            buffStr);
    }

    // 复活动作：满血、翻倍飞返次数、结束玩家回合
    private async Task RespawnMove(IReadOnlyList<Creature> targets)
    {
        // 1. 满血复活
        await CreatureCmd.Heal(Creature, Creature.MaxHp);

        // 2. 翻倍所有玩家的飞返次数
        FlybackManager.DoubleAllPlayerCounts();

        // 3. 刷新 RetrogradePower 的生命值加成
        var retro = Creature.GetPower<RetrogradePower>();
        if (retro != null)
            await retro.RefreshHpBoost();

        // 4. 重新添加因死亡丢失的 TimeWatchPower
        var timeWatch = Creature.GetPower<TimeWatchPower>();
        if (timeWatch == null)
        {
            await PowerCmd.Apply<TimeWatchPower>(
                new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        }

        // 5. 强制播放闲置动画（修复卡在死亡动画的问题）
        await CreatureCmd.TriggerAnim(Creature, "idle_loop", 0.0f);

        // 6. 移除复活能力，确保第二次死亡正常结束战斗
        if (retro != null)
            await PowerCmd.Remove<RetrogradePower>(Creature);

        // 7. 结束当前玩家回合
        var combatState = Creature.CombatState;
        if (combatState?.CurrentSide == MegaCrit.Sts2.Core.Combat.CombatSide.Player)
        {
            var currentPlayer = MegaCrit.Sts2.Core.Context.LocalContext.GetMe(combatState);
            if (currentPlayer != null)
                PlayerCmd.EndTurn(currentPlayer, false);
        }
    }
    

    // 普通移动，保持不变
    private async Task DoubleAttackMove(IReadOnlyList<Creature> targets)
    {
        for (int i = 0; i < 2; i++)
            await DamageCmd.Attack(10).FromMonster(this).Execute(null);
        var player = targets.FirstOrDefault()?.Player;
        if (player != null)
            await CardPileCmd.AddToCombatAndPreview<Flyback>(targets, PileType.Draw, 5, null);
    }

    private async Task HeavyAttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(30).FromMonster(this).Execute(null);
    }

    private async Task BuffStrengthMove(IReadOnlyList<Creature> targets)
    {
        int playCount = FlybackManager.Instance.TotalPlayCount;
        int reloads = GetReloadCount();
        int amount = 1 + (int)((playCount / 100f) * reloads);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, amount, Creature, null);
    }

    private async Task BuffStrengthMove2(IReadOnlyList<Creature> targets)
    {
        int playCount = FlybackManager.Instance.TotalPlayCount;
        int reloads = GetReloadCount();
        int amount = 2 + (int)((playCount / 100f) * reloads);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, amount, Creature, null);
    }

    private static int GetReloadCount()
    {
        var field = typeof(RunManager).GetField("_numReloads", BindingFlags.NonPublic | BindingFlags.Instance);
        return field != null ? (int)field.GetValue(RunManager.Instance) : 0;
    }
}