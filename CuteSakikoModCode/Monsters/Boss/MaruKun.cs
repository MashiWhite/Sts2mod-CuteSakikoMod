using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Boss;

/// <summary>
///     丸君：藤都子的手偶，也是她的"最好的朋友"。开局就在场，是 MinionPower 爪牙。
///     意图普通：拍一下 / 拍一下并上易伤。
/// </summary>
[RegisterMonster]
public class MaruKun : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 60, 55);
    public override int MaxInitialHp => MinInitialHp;

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/maru_kun.tscn"
    );

    private int HitDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);
    private int GentleHitDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 6);
    private const int VulnerableAmount = 1;

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<MinionPower>(
            new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var hit = new MoveState("MARU_HIT", HitMove, new SingleAttackIntent(HitDamage));
        var gentleHit = new MoveState("MARU_GENTLE_HIT", GentleHitMove,
            new SingleAttackIntent(GentleHitDamage), new DebuffIntent());

        var random = new RandomBranchState("MARU_RANDOM");
        random.AddBranch(hit, MoveRepeatType.CanRepeatForever, 0.5f);
        random.AddBranch(gentleHit, MoveRepeatType.CanRepeatForever, 0.5f);

        hit.FollowUpState = random;
        gentleHit.FollowUpState = random;

        var states = new List<MonsterState> { random, hit, gentleHit };
        return new MonsterMoveStateMachine(states, random);
    }

    private async Task HitMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(HitDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .Execute(null);
    }

    private async Task GentleHitMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(GentleHitDamage)
            .FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .Execute(null);

        var state = Creature.CombatState;
        if (state == null) return;

        foreach (var player in state.Players)
            await PowerCmd.Apply<VulnerablePower>(
                new ThrowingPlayerChoiceContext(), player.Creature, VulnerableAmount, Creature, null);
    }
}
