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

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Boss.Tenday;

[RegisterMonster]
public class TenDayUmirin : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 100, 75);
    public override int MaxInitialHp => MinInitialHp;

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/tenday/tendayumirin.tscn"
    );

    private int DebuffAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
        => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var candidates = new[] { "APPLY_VULNERABLE", "APPLY_FRAIL" };

        var vuln  = new RandomMoveState("APPLY_VULNERABLE", VulnMove,  candidates, new DebuffIntent());
        var frail = new RandomMoveState("APPLY_FRAIL",      FrailMove, candidates, new DebuffIntent());

        var states = new List<MonsterState> { vuln, frail };
        return new MonsterMoveStateMachine(states, vuln);
    }

    private async Task VulnMove(IReadOnlyList<Creature> targets)
    {
        foreach (var player in Creature.CombatState.Players)
            await PowerCmd.Apply<VulnerablePower>(
                new ThrowingPlayerChoiceContext(), player.Creature, DebuffAmount, Creature, null);
    }

    private async Task FrailMove(IReadOnlyList<Creature> targets)
    {
        foreach (var player in Creature.CombatState.Players)
            await PowerCmd.Apply<FrailPower>(
                new ThrowingPlayerChoiceContext(), player.Creature, DebuffAmount, Creature, null);
    }
}