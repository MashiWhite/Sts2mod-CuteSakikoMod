
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Boss.TenDay;

[RegisterMonster]
public class TenDayTaki : ModMonsterTemplate
{
    public override int MinInitialHp => 800;
    public override int MaxInitialHp => 800;

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/tenday/tendaytaki.tscn"
    );

    private int Damage1 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 20, 16);
    private int Damage2 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 7, 5);
    private int Damage3 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 10);
    private int Block3 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 35, 30);
    private int Block4 => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 60, 50);

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        var ctx = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<MovementNotFinishedPower>(ctx, Creature, 1, Creature, null);
        await PowerCmd.Apply<TenDayEndPower>(ctx, Creature, 10, Creature, null);
    }

    public async Task SummonAlly<T>(PlayerChoiceContext ctx, string slotName) where T : MonsterModel
    {
        var combatState = Creature.CombatState;
        if (combatState == null) return;
        if (combatState.Enemies.Any(e => e.SlotName == slotName)) return;

        var ally = await CreatureCmd.Add<T>(combatState, slotName);
        await PowerCmd.Apply<MinionPower>(
            new ThrowingPlayerChoiceContext(), ally, 1, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var candidates = new[] { "ATTACK_1", "ATTACK_2", "ATTACK_3", "BLOCK_4" };

        var attack1 = new RandomMoveState("ATTACK_1", Attack1Move, candidates,
            new SingleAttackIntent(Damage1));
        var attack2 = new RandomMoveState("ATTACK_2", Attack2Move, candidates,
            new MultiAttackIntent(Damage2, 2));
        var attack3 = new RandomMoveState("ATTACK_3", Attack3Move, candidates,
            new SingleAttackIntent(Damage3), new DefendIntent());
        var block4  = new RandomMoveState("BLOCK_4",  Block4Move,  candidates,
            new DefendIntent());

        var states = new List<MonsterState> { attack1, attack2, attack3, block4 };
        return new MonsterMoveStateMachine(states, attack1);
    }

    private async Task Attack1Move(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(Damage1).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task Attack2Move(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(Damage2).FromMonster(this)
            .WithHitCount(2)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task Attack3Move(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(Damage3).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
        await CreatureCmd.GainBlock(Creature, Block3, ValueProp.Move, null);
    }

    private async Task Block4Move(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, Block4, ValueProp.Move, null);
    }
}