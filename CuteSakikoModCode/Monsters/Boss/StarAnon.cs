using CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Event;
using CuteSakikoMod.CuteSakikoModCode.Nodes.Combat;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Boss;

[RegisterMonster]
public class StarAnon : ModMonsterTemplate
{
    private MoveState _deadState;
    private string _lastMoveName = "";
#if STS2_AT_LEAST_0_113_0
    private static bool IsDread => AscensionHelper.IsDread();
#else
    private static bool IsDread => false;
#endif

    private static int DreadBonus(int amount) => IsDread ? amount : 0;

    private int AttackDamage => 12 + DreadBonus(3);
    private int HeavyDamage => 28 + DreadBonus(3);

    public override bool ShouldDisappearFromDoom => Creature?.GetPower<RetrogradePower>() == null;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 220, 120) + DreadBonus(40);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 260, 160) + DreadBonus(40);

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/star_anon_boss.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<RetrogradePower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        await PowerCmd.Apply<TimeWatchPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);

        // 主机同步基础 ReloadCount（广播）
        if (RunManager.Instance.NetService.Type == NetGameType.Host)
            FlybackManager.SyncReloadCountIfHost();
    }

    public async Task TriggerDeadState()
    {
        SetMoveImmediate(_deadState, true);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        _deadState = new MoveState("RESPAWN_MOVE", RespawnMove, new HealIntent(), new BuffIntent(), new FlybackDoubleIntent())
        {
            MustPerformOnceBeforeTransitioning = true
        };

        var buffStr = new MoveState("BUFF_STRENGTH", BuffStrengthMove, new BuffIntent(), new ReloadCountIntent(2 + DreadBonus(1)), new FlybackCountIntent(25));
        var attack1 = new MoveState("DOUBLE_ATTACK", DoubleAttackMove,
            new MultiAttackIntent(AttackDamage, 2), new StatusIntent(2));
        var heavy1 = new MoveState("HEAVY_ATTACK", HeavyAttackMove,
            new SingleAttackIntent(HeavyDamage));
        var buffStr2 = new MoveState("BUFF_STRENGTH2", BuffStrengthMove2, new BuffIntent(), new ReloadCountIntent(1 + DreadBonus(1)));

        buffStr.FollowUpState = attack1;
        attack1.FollowUpState = heavy1;
        heavy1.FollowUpState = buffStr2;
        buffStr2.FollowUpState = attack1;

        var buffStr3 = new MoveState("BUFF_STRENGTH3", BuffStrengthMove3, new BuffIntent(), new ReloadCountIntent(2 + DreadBonus(1)));
        var attack3 = new MoveState("DOUBLE_ATTACK3", DoubleAttackMove,
            new MultiAttackIntent(AttackDamage, 2), new StatusIntent(2));
        var heavy3 = new MoveState("HEAVY_ATTACK3", HeavyAttackMove,
            new SingleAttackIntent(HeavyDamage));

        _deadState.FollowUpState = buffStr3;
        buffStr3.FollowUpState = attack3;
        attack3.FollowUpState = heavy3;
        heavy3.FollowUpState = buffStr3;

        return new MonsterMoveStateMachine(
            new List<MonsterState>
            {
                _deadState,
                buffStr, attack1, heavy1, buffStr2,
                buffStr3, attack3, heavy3
            },
            buffStr);
    }

    private async Task RespawnMove(IReadOnlyList<Creature> targets)
    {
        _lastMoveName = "RESPAWN_MOVE";
        await CreatureCmd.Heal(Creature, Creature.MaxHp);
        foreach (var player in RunManager.Instance.DebugOnlyGetState()?.Players ?? Enumerable.Empty<Player>())
            for (var i = 0; i < 15; i++)
                FlybackManager.Instance.IncrementPlayCountForPlayer(player);
        FlybackManager.DoubleAllPlayerCounts();

        var retro = Creature.GetPower<RetrogradePower>();
        if (retro != null)
            await retro.RefreshHpBoost();

        var timeWatch = Creature.GetPower<TimeWatchPower>();
        if (timeWatch == null)
            await PowerCmd.Apply<TimeWatchPower>(
                new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);

        await CreatureCmd.TriggerAnim(Creature, "idle_loop", 0.0f);

        if (retro != null)
            await PowerCmd.Remove<RetrogradePower>(Creature);

        await PowerCmd.Apply<LastRetrogradePower>(
            new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    private async Task BuffStrengthMove(IReadOnlyList<Creature> targets)
    {
        _lastMoveName = "BUFF_STRENGTH";
        var reloads = FlybackManager.GetReloadCount();
        for (var i = 0; i < 2 + DreadBonus(1); i++) await SyncFlybackDataForMove();
        var playCount = FlybackManager.Instance.TotalPlayCount;
        // ★ 飞返计数只加「全局总量」，不碰任何单独玩家的计数
        FlybackManager.Instance.AddGlobalBonusPlayCount(25);
        var amount = 2 + (int)(playCount / 100f * reloads);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, amount, Creature, null);
    }

    private async Task BuffStrengthMove2(IReadOnlyList<Creature> targets)
    {
        _lastMoveName = "BUFF_STRENGTH2";
        var reloads = FlybackManager.GetReloadCount();
        for (var i = 0; i < 1 + DreadBonus(1); i++) await SyncFlybackDataForMove();
        var playCount = FlybackManager.Instance.TotalPlayCount;
        var amount = 2 + (int)(playCount / 100f * reloads);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, amount, Creature, null);
    }

    private async Task BuffStrengthMove3(IReadOnlyList<Creature> targets)
    {
        _lastMoveName = "BUFF_STRENGTH3";
        var reloads = FlybackManager.GetReloadCount();
        for (var i = 0; i < 2 + DreadBonus(1); i++) await SyncFlybackDataForMove();
        var playCount = FlybackManager.Instance.TotalPlayCount;
        var amount = 2 + (int)(playCount / 100f * reloads);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, amount, Creature, null);
    }

    // ★ 关键：不再区分主客机，不再有任何等待
    private async Task SyncFlybackDataForMove()
    {
        FlybackManager.IncrementReloadCount();
    }

    private async Task DoubleAttackMove(IReadOnlyList<Creature> targets)
    {
        _lastMoveName = "DOUBLE_ATTACK";
        for (var i = 0; i < 2; i++)
            await DamageCmd.Attack(AttackDamage).FromMonster(this).Execute(null);
        var player = targets.FirstOrDefault()?.Player;
        if (player != null)
            await CardPileCmd.AddToCombatAndPreview<Flyback>(targets, IsDread ? PileType.Draw : PileType.Discard, 2, null,
                CardPilePosition.Random);
    }

    private async Task HeavyAttackMove(IReadOnlyList<Creature> targets)
    {
        _lastMoveName = "HEAVY_ATTACK";
        await DamageCmd.Attack(HeavyDamage).FromMonster(this).Execute(null);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy)
            return;
        _lastMoveName = "";
    }
}