
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Boss.TenDay;

[RegisterMonster]
public class TenDaySaki : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 100, 75);
    public override int MaxInitialHp => MinInitialHp;

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/tenday/tendaysaki.tscn"
    );

    private int WeakAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);
    private int Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 10, 8);

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
        => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var candidates = new[] { "APPLY_WEAK", "ATTACK" };

        var weak   = new RandomMoveState("APPLY_WEAK", WeakMove,   candidates, new DebuffIntent());
        var attack = new RandomMoveState("ATTACK",     AttackMove, candidates, new SingleAttackIntent(Damage));

        var states = new List<MonsterState> { weak, attack };
        return new MonsterMoveStateMachine(states, weak);
    }

    private async Task WeakMove(IReadOnlyList<Creature> targets)
    {
        foreach (var player in Creature.CombatState.Players)
            await PowerCmd.Apply<WeakPower>(
                new ThrowingPlayerChoiceContext(), player.Creature, WeakAmount, Creature, null);
    }

    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(Damage).FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }
}