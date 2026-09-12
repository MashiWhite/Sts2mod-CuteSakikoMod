using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Boss.ChocolateSnail;

[RegisterMonster]
public class SmallChocolateSnail : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 15, 12);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 20, 15);

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/small_chocolate_snail.tscn"
    );

    private int Attack2Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);
    private int Attack3HitCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
    private int Attack6Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 6);

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        // 随机 1~10 层美味
        var rng = Creature.CombatState.RunState.Rng.Shuffle;
        int deliciousAmount = rng.NextInt(1, 11);
        await PowerCmd.Apply<DeliciousPower>(
            new ThrowingPlayerChoiceContext(), Creature, deliciousAmount, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var attack2 = new MoveState("ATTACK_2", Attack2Move, new MultiAttackIntent(Attack2Damage, 2));
        var attack3 = new MoveState("ATTACK_3", Attack3Move, new MultiAttackIntent(1, Attack3HitCount));
        var attack6 = new MoveState("ATTACK_6", Attack6Move, new SingleAttackIntent(Attack6Damage));

        var randomState = new RandomBranchState("SMALL_SNAIL_RANDOM");
        randomState.AddBranch(attack2, MoveRepeatType.CanRepeatForever, 1f / 3f);
        randomState.AddBranch(attack3, MoveRepeatType.CanRepeatForever, 1f / 3f);
        randomState.AddBranch(attack6, MoveRepeatType.CanRepeatForever, 1f / 3f);

        attack2.FollowUpState = randomState;
        attack3.FollowUpState = randomState;
        attack6.FollowUpState = randomState;

        var states = new List<MonsterState> { randomState, attack2, attack3, attack6 };
        return new MonsterMoveStateMachine(states, randomState);
    }

    private async Task Attack2Move(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(Attack2Damage)
            .FromMonster(this)
            .WithHitCount(2)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task Attack3Move(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(1)
            .FromMonster(this)
            .WithHitCount(Attack3HitCount)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task Attack6Move(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(Attack6Damage)
            .FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}