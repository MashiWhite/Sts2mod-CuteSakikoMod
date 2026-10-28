using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using Godot;
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

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Elite;

/// <summary>
///     薇欧拉（第 2 幕精英）。中提琴手，擅长用"甜言蜜语"让人看不清手里的牌。
///     机制「欺诈之蛇」：被施加后，你每回合抽到的前 N 张牌无法辨认（卡图/标题/描述被替换）。
///     循环：蛇之凝视 → 欺诈之蛇 → 不协和音 → 蜕皮，侵蚀每 4 回合一次。
/// </summary>
[RegisterMonster]
public class Viola : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 165, 155);
    public override int MaxInitialHp => MinInitialHp;

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/viola.tscn"
    );

    private int GazeDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 15, 13);
    private int DissonanceDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 8, 7);
    private int MoltBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 14, 12);

    private const int WeakAmount = 2;
    private const int FrailAmount = 2;
    private const int StrengthAmount = 2;

    /// <summary>欺诈之蛇层数：低进阶 1，高进阶 2。</summary>
    private int SerpentAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var gaze = new MoveState("SERPENT_GAZE", GazeMove,
            new SingleAttackIntent(GazeDamage), new DebuffIntent());
        var serpent = new MoveState("FRAUDULENT_SERPENT", FraudulentSerpentMove,
            new DebuffIntent());
        var dissonance = new MoveState("DISSONANCE", DissonanceMove,
            new MultiAttackIntent(DissonanceDamage, 2), new DebuffIntent());
        var molt = new MoveState("MOLT", MoltMove,
            new DefendIntent(), new BuffIntent());

        gaze.FollowUpState = serpent;
        serpent.FollowUpState = dissonance;
        dissonance.FollowUpState = molt;
        molt.FollowUpState = gaze;

        var states = new List<MonsterState> { gaze, serpent, dissonance, molt };
        return new MonsterMoveStateMachine(states, gaze);
    }

    private async Task GazeMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(GazeDamage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        var state = Creature.CombatState;
        if (state == null) return;

        foreach (var player in state.Players)
            await PowerCmd.Apply<WeakPower>(
                new ThrowingPlayerChoiceContext(), player.Creature, WeakAmount, Creature, null);
    }

    private async Task FraudulentSerpentMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        var state = Creature.CombatState;
        if (state == null) return;

        foreach (var player in state.Players)
            await PowerCmd.Apply<FraudulentSerpentPower>(
                new ThrowingPlayerChoiceContext(), player.Creature, SerpentAmount, Creature, null);
    }

    private async Task DissonanceMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(DissonanceDamage).FromMonster(this)
            .WithHitCount(2)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        var state = Creature.CombatState;
        if (state == null) return;

        foreach (var player in state.Players)
            await PowerCmd.Apply<FrailPower>(
                new ThrowingPlayerChoiceContext(), player.Creature, FrailAmount, Creature, null);
    }

    private async Task MoltMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, MoltBlock, ValueProp.Move, null);
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), Creature, StrengthAmount, Creature, null);
    }
}
