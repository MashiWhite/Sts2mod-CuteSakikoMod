using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Boss;

[RegisterMonster]
public class GreyAnon : ModMonsterTemplate
{
    private bool _isPhaseTwo;
    private MoveState _performState;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 730, 700);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 830, 800);

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/grey_anon_boss.tscn"
    );

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }
    
    // 已有的公共方法（用于 Live 视觉）
    public NCreatureVisuals? CreateLiveVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(
            "res://CuteSakikoMod/scenes/monster/grey_anon_boss_live.tscn");
    }
    
    // 新增：公共方法用于创建原始视觉（提供给 AiHeartPower 恢复时使用）
    public NCreatureVisuals? CreateOriginalVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }
    
    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        await PowerCmd.Apply<HeartWallPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
        await PowerCmd.Apply<BecomeAshesPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // ----- 血量检测与阶段切换 -----
    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Creature) return;
        if (!_isPhaseTwo && Creature.CurrentHp <= MaxInitialHp * 0.5)
        {
            _isPhaseTwo = true;
            await PowerCmd.Apply<BecomeAshesPower>(new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
            // 强制切换到第二阶段初始意图
            SetMoveImmediate(_performState, true);
        }
    }

    // ----- 状态机 -----
    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // -- 第一阶段 --
        var monologue = new MoveState("MONOLOGUE", MonologueMove,
            new SingleAttackIntent(8));
        var heavy1 = new MoveState("HEAVY_ATTACK_1", HeavyAttack1Move,
            new SingleAttackIntent(12), new DefendIntent());
        var devote1 = new MoveState("DEVOTE_1", Devote1Move,
            new DebuffIntent(), new HealIntent());
        var buff1 = new MoveState("BUFF_1", Buff1Move,
            new SingleAttackIntent(6), new DebuffIntent());

        monologue.FollowUpState = heavy1;
        heavy1.FollowUpState = devote1;
        devote1.FollowUpState = buff1;
        buff1.FollowUpState = heavy1;

        // -- 第二阶段 --
        _performState = new MoveState("PERFORM", PerformMove,
            new DebuffIntent(), new DefendIntent());
        var heavy2 = new MoveState("HEAVY_ATTACK_2", HeavyAttack2Move,
            new SingleAttackIntent(10), new DefendIntent());
        var devote2 = new MoveState("DEVOTE_2", Devote2Move,
            new DebuffIntent(), new HealIntent());

        _performState.FollowUpState = heavy2;
        heavy2.FollowUpState = devote2;
        devote2.FollowUpState = _performState;

        var states = new List<MonsterState>
        {
            monologue, heavy1, devote1, buff1,
            _performState, heavy2, devote2
        };

        return new MonsterMoveStateMachine(states, monologue);
    }

    // ========== 招式实现 ==========
    private async Task MonologueMove(IReadOnlyList<Creature> targets)
    {
        TalkCmd.Play(MonsterModel.L10NMonsterLookup("CUTE_SAKIKO_MOD_MONSTER_GREY_ANON.monologue"), Creature, VfxColor.Blue);
        await DamageCmd.Attack(8).FromMonster(this).Execute(null);
    }

    private async Task HeavyAttack1Move(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(12).FromMonster(this).Execute(null);
        await CreatureCmd.GainBlock(Creature, 20, ValueProp.Move, null);
    }

    private async Task Devote1Move(IReadOnlyList<Creature> targets)
    {
        foreach (var player in Creature.CombatState.Players)
            await PowerCmd.Apply<DexterityPower>(new ThrowingPlayerChoiceContext(),
                player.Creature, -1, Creature, null, true);

        foreach (var player in Creature.CombatState.Players)
            await CreatureCmd.Heal(player.Creature, 10);
    }

    private async Task Buff1Move(IReadOnlyList<Creature> targets)
    {
        foreach (var player in Creature.CombatState.Players)
            await PowerCmd.Apply<VulnerablePower>(new ThrowingPlayerChoiceContext(),
                player.Creature, 3, Creature, null);

        await DamageCmd.Attack(6).FromMonster(this).Execute(null);
    }

    private async Task PerformMove(IReadOnlyList<Creature> targets)
    {
        var rng = Creature.CombatState.RunState.Rng.Shuffle;
        var randomLine = LocString.GetRandomWithPrefix("monsters", "CUTE_SAKIKO_MOD_MONSTER_GREY_ANON.perform", rng);
        if (randomLine != null && !randomLine.IsEmpty)
            TalkCmd.Play(randomLine, Creature, VfxColor.Blue);

        foreach (var player in Creature.CombatState.Players)
            await PowerCmd.Apply<StrengthPower>(new ThrowingPlayerChoiceContext(),
                player.Creature, -2, Creature, null, true);

        await CreatureCmd.GainBlock(Creature, 15, ValueProp.Move, null);
    }

    private async Task HeavyAttack2Move(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(10).FromMonster(this).Execute(null);
        await CreatureCmd.GainBlock(Creature, 35, ValueProp.Move, null);
    }

    private async Task Devote2Move(IReadOnlyList<Creature> targets)
    {
        foreach (var player in Creature.CombatState.Players)
            await PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(),
                player.Creature, 3, Creature, null);

        foreach (var player in Creature.CombatState.Players)
            await CreatureCmd.Heal(player.Creature, 5);
    }
}