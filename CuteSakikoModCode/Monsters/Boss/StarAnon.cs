using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Token;
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
using MegaCrit.Sts2.Core.Multiplayer;
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

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 440, 420);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 520, 520);

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

        if (RunManager.Instance.NetService.Type == NetGameType.Host)
        {
            FlybackManager.SyncReloadCountIfHost();
            FlybackManager.SyncPlayCountIfHost();
        }
    }

    public async Task TriggerDeadState()
    {
        SetMoveImmediate(_deadState, true);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        _deadState = new MoveState("RESPAWN_MOVE", RespawnMove, new HealIntent(), new BuffIntent())
        {
            MustPerformOnceBeforeTransitioning = true
        };

        var buffStr = new MoveState("BUFF_STRENGTH", BuffStrengthMove, new BuffIntent());
        var attack1 = new MoveState("DOUBLE_ATTACK", DoubleAttackMove,
            new MultiAttackIntent(10, 2), new StatusIntent(2));
        var heavy1 = new MoveState("HEAVY_ATTACK", HeavyAttackMove,
            new SingleAttackIntent(20));
        var buffStr2 = new MoveState("BUFF_STRENGTH2", BuffStrengthMove2, new BuffIntent());

        buffStr.FollowUpState = attack1;
        attack1.FollowUpState = heavy1;
        heavy1.FollowUpState = buffStr2;
        buffStr2.FollowUpState = attack1;

        var buffStr3 = new MoveState("BUFF_STRENGTH3", BuffStrengthMove3, new BuffIntent());
        var attack3 = new MoveState("DOUBLE_ATTACK3", DoubleAttackMove,
            new MultiAttackIntent(10, 2), new StatusIntent(2));
        var heavy3 = new MoveState("HEAVY_ATTACK3", HeavyAttackMove,
            new SingleAttackIntent(20));

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
        FlybackManager.DoubleAllPlayerCounts();

        if (RunManager.Instance.NetService.Type == NetGameType.Client)
        {
            await FlybackManager.WaitForPlayCountChange(timeoutMs: 500);
        }

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
        await SyncFlybackDataForMove();

        int playCount = FlybackManager.Instance.TotalPlayCount;
        int reloads = GetReloadCount();
        int amount = 1 + (int)(playCount / 100f * reloads);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, amount, Creature, null);

        foreach (var player in RunManager.Instance.DebugOnlyGetState()?.Players ?? Enumerable.Empty<Player>())
        {
            for (int i = 0; i < 5; i++)
                FlybackManager.Instance.IncrementPlayCountForPlayer(player);
        }
    }

    private async Task BuffStrengthMove2(IReadOnlyList<Creature> targets)
    {
        _lastMoveName = "BUFF_STRENGTH2";
        int playCount = FlybackManager.Instance.TotalPlayCount;
        int reloads = GetReloadCount();
        int amount = 2 + (int)(playCount / 100f * reloads);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, amount, Creature, null);
    }

    private async Task BuffStrengthMove3(IReadOnlyList<Creature> targets)
    {
        _lastMoveName = "BUFF_STRENGTH3";
        await SyncFlybackDataForMove();

        int playCount = FlybackManager.Instance.TotalPlayCount;
        int reloads = GetReloadCount();
        int amount = 2 + (int)(playCount / 100f * reloads);
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(), Creature, amount, Creature, null);
    }

    private async Task SyncFlybackDataForMove()
    {
        if (RunManager.Instance.NetService.Type == NetGameType.Host)
        {
            FlybackManager.IncrementReloadCount();
            FlybackManager.SyncPlayCountIfHost();
        }
        else
        {
            int expected = GetReloadCount() + 1;
            await FlybackManager.WaitForReloadCountV2(expected, timeoutMs: 1000);
            await FlybackManager.WaitForPlayCountChange(timeoutMs: 500);
        }
    }

    private async Task DoubleAttackMove(IReadOnlyList<Creature> targets)
    {
        _lastMoveName = "DOUBLE_ATTACK";
        for (var i = 0; i < 2; i++)
            await DamageCmd.Attack(10).FromMonster(this).Execute(null);
        var player = targets.FirstOrDefault()?.Player;
        if (player != null)
            await CardPileCmd.AddToCombatAndPreview<Flyback>(targets, PileType.Discard, 2, null, CardPilePosition.Random);
    }

    private async Task HeavyAttackMove(IReadOnlyList<Creature> targets)
    {
        _lastMoveName = "HEAVY_ATTACK";
        await DamageCmd.Attack(20).FromMonster(this).Execute(null);
    }

    // ★ 修改为 AfterSideTurnEnd，匹配新版 AbstractModel
    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy)
            return;

        _lastMoveName = "";
    }

    private static int GetReloadCount()
    {
        return FlybackManager.GetReloadCount();
    }
}