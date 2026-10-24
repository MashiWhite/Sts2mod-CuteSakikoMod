using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Boss.Tenday;

[RegisterMonster]
public class TenDayAnon : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 100, 75);
    public override int MaxInitialHp => MinInitialHp;

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/tenday/tendayanon.tscn"
    );

    private decimal HealPercent => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 0.10m, 0.05m);
    private int BlockAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 10);

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
        => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var candidates = new[] { "HEAL_ALLIES", "BLOCK_ALLIES" };

        var heal  = new RandomMoveState("HEAL_ALLIES",  HealAlliesMove,  candidates, new HealIntent());
        var block = new RandomMoveState("BLOCK_ALLIES", BlockAlliesMove, candidates, new DefendIntent());

        var states = new List<MonsterState> { heal, block };
        return new MonsterMoveStateMachine(states, heal);
    }

    private async Task HealAlliesMove(IReadOnlyList<Creature> targets)
    {
        var taki = Creature.CombatState.Enemies
            .FirstOrDefault(e => e.Monster is TenDayTaki);
        if (taki == null) return;
        var heal = (int)(taki.CurrentHp * HealPercent);

        foreach (var ally in Creature.CombatState.GetTeammatesOf(Creature).Where(c => c.IsAlive))
            await CreatureCmd.Heal(ally, heal);
    }

    private async Task BlockAlliesMove(IReadOnlyList<Creature> targets)
    {
        foreach (var ally in Creature.CombatState.GetTeammatesOf(Creature).Where(c => c.IsAlive))
            await CreatureCmd.GainBlock(ally, BlockAmount, ValueProp.Move, null);
    }
}