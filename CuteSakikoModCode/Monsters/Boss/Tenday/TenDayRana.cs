
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
public class TenDayRana : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 100, 75);
    public override int MaxInitialHp => MinInitialHp;

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/tenday/tendayrana.tscn"
    );

    private int HitCount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);
    private int StrengthAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
        => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var candidates = new[] { "MULTI_HIT", "BUFF_TEAM" };

        var multi = new RandomMoveState("MULTI_HIT", MultiHitMove, candidates,
            new MultiAttackIntent(1, HitCount));
        var buff  = new RandomMoveState("BUFF_TEAM", BuffTeamMove, candidates,
            new BuffIntent());

        var states = new List<MonsterState> { multi, buff };
        return new MonsterMoveStateMachine(states, multi);
    }

    private async Task MultiHitMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(1).FromMonster(this)
            .WithHitCount(HitCount)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task BuffTeamMove(IReadOnlyList<Creature> targets)
    {
        foreach (var ally in Creature.CombatState.GetTeammatesOf(Creature).Where(c => c.IsAlive))
            await PowerCmd.Apply<StrengthPower>(
                new ThrowingPlayerChoiceContext(), ally, StrengthAmount, Creature, null);
    }
}