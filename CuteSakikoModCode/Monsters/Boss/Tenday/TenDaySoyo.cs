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
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Boss.Tenday;

[RegisterMonster]
public class TenDaySoyo : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 100, 75);
    public override int MaxInitialHp => MinInitialHp;

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/tenday/tendaysoyo.tscn"
    );

    private int DexAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);
    private int BlockHits => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
        => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var candidates = new[] { "BUFF_DEX", "BLOCK_TEAM" };

        var buff  = new RandomMoveState("BUFF_DEX",   BuffDexMove,   candidates, new BuffIntent());
        var block = new RandomMoveState("BLOCK_TEAM", BlockTeamMove, candidates, new DefendIntent());

        var states = new List<MonsterState> { buff, block };
        return new MonsterMoveStateMachine(states, buff);
    }

    private async Task BuffDexMove(IReadOnlyList<Creature> targets)
    {
        foreach (var ally in Creature.CombatState.GetTeammatesOf(Creature).Where(c => c.IsAlive))
            await PowerCmd.Apply<DexterityPower>(
                new ThrowingPlayerChoiceContext(), ally, DexAmount, Creature, null);
    }

    private async Task BlockTeamMove(IReadOnlyList<Creature> targets)
    {
        for (int i = 0; i < BlockHits; i++)
        {
            foreach (var ally in Creature.CombatState.GetTeammatesOf(Creature).Where(c => c.IsAlive))
                await CreatureCmd.GainBlock(ally, 2, ValueProp.Move, null);
        }
    }
}