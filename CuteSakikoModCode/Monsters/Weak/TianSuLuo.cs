using System.Reflection;
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

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Weak;

[RegisterMonster]
public class TianSuLuo : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 40, 35);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 50, 45);

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/tiansuluo.tscn"
    );

    // 高进阶时伤害和格挡 +1
    private int OhYeahDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 4);
    private int OhYeahBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 5, 3);
    private int ActingCuteBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);
    private int HappyBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var ohYeah = new MoveState("OH_YEAH", OhYeahMove,
            new SingleAttackIntent(OhYeahDamage), new DefendIntent());
        var actingCute = new MoveState("ACTING_CUTE", ActingCuteMove,
            new DefendIntent(), new DebuffIntent());
        var happy = new MoveState("HAPPY", HappyMove,
            new BuffIntent(), new DefendIntent());

        // 按顺序循环：哦耶 → 卖萌 → 高兴 → 哦耶 ...
        ohYeah.FollowUpState = actingCute;
        actingCute.FollowUpState = happy;
        happy.FollowUpState = ohYeah;

        var states = new List<MonsterState> { ohYeah, actingCute, happy };
        return new MonsterMoveStateMachine(states, ohYeah);
    }

    private async Task OhYeahMove(IReadOnlyList<Creature> targets)
    {
        // 播放音效
        PlayOhYeahSound();

        await DamageCmd.Attack(OhYeahDamage).FromMonster(this).Execute(null);
        await CreatureCmd.GainBlock(Creature, OhYeahBlock, ValueProp.Move, null);
    }

    private async Task ActingCuteMove(IReadOnlyList<Creature> targets)
    {
        PlayOhYeahSound();
        
        await CreatureCmd.GainBlock(Creature, ActingCuteBlock, ValueProp.Move, null);

        foreach (var player in Creature.CombatState.Players)
            await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(),
                player.Creature, 1, Creature, null);
    }

    private async Task HappyMove(IReadOnlyList<Creature> targets)
    {
        PlayOhYeahSound();
        
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(),
            Creature, 1, Creature, null);
        await CreatureCmd.GainBlock(Creature, HappyBlock, ValueProp.Move, null);
    }

    private static void PlayOhYeahSound()
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var fullPath = Path.Combine(dir, "audio", "ohyeah.mp3");
        AudioManager.PlaySound(fullPath);
    }
}