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
public class TianXiangLuo : ModMonsterTemplate
{
    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 35, 30);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 45, 40);

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/tianxiangluo.tscn"
    );

    // 高进阶时伤害和格挡 +1
    private int DesuwaDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);
    private int DesuwaBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 2);
    private int ActingCuteBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);
    private int HappyBlock => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var desuwa = new MoveState("DESUWA", DesuwaMove,
            new SingleAttackIntent(DesuwaDamage), new DefendIntent());
        var actingCute = new MoveState("ACTING_CUTE", ActingCuteMove,
            new DefendIntent(), new DebuffIntent());
        var happy = new MoveState("HAPPY", HappyMove,
            new BuffIntent(), new DefendIntent());
        
        desuwa.FollowUpState = actingCute;
        actingCute.FollowUpState = happy;
        happy.FollowUpState = desuwa;

        var states = new List<MonsterState> { desuwa, actingCute, happy };
        return new MonsterMoveStateMachine(states, desuwa);
    }

    private async Task DesuwaMove(IReadOnlyList<Creature> targets)
    {
        // 播放音效
        PlayDesuwaSound();

        await DamageCmd.Attack(DesuwaDamage).FromMonster(this).Execute(null);
        await CreatureCmd.GainBlock(Creature, DesuwaBlock, ValueProp.Move, null);
    }

    private async Task ActingCuteMove(IReadOnlyList<Creature> targets)
    {
        PlayDesuwaSound();
        
        await CreatureCmd.GainBlock(Creature, ActingCuteBlock, ValueProp.Move, null);

        foreach (var player in Creature.CombatState.Players)
            await PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(),
                player.Creature, 1, Creature, null);
    }

    private async Task HappyMove(IReadOnlyList<Creature> targets)
    {
        PlayDesuwaSound();
        
        await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(),
            Creature, 2, Creature, null);
        await CreatureCmd.GainBlock(Creature, HappyBlock, ValueProp.Move, null);
    }

    private static void PlayDesuwaSound()
    {
        var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var fullPath = Path.Combine(dir, "audio", "desuwa.mp3");
        AudioManager.PlaySound(fullPath);
    }
}