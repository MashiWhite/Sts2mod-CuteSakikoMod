
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

using MegaCrit.Sts2.Core.Helpers;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Weak;

[RegisterMonster]
public class AnonWang : ModMonsterTemplate
{
    public override int MinInitialHp =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 38, 35);

    public override int MaxInitialHp =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 43, 40);

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/anonwang.tscn"
    );

    private int Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 5);
    private int Block => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 6);
    private int StrengthAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);
    private int DexterityAmount => 1;

    private MoveState _attackState = null!;
    private MoveState _defendState = null!;
    private MoveState _buffState = null!;
    private MoveState[] _allStates = null!;

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
        => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        var ctx = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<ShowOffPower>(ctx, Creature, 1, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        _attackState = new MoveState("ATTACK", AttackMove, new SingleAttackIntent(Damage));
        _defendState = new MoveState("DEFEND", DefendMove, new DefendIntent());
        _buffState = new MoveState("BUFF", BuffMove, new BuffIntent());

        // 无干扰时循环 123
        _attackState.FollowUpState = _defendState;
        _defendState.FollowUpState = _buffState;
        _buffState.FollowUpState = _attackState;

        _allStates = new[] { _attackState, _defendState, _buffState };

        var states = new List<MonsterState> { _attackState, _defendState, _buffState };
        return new MonsterMoveStateMachine(states, _attackState);
    }

    /// <summary>
    /// 供 ShowOffPower 调用，根据玩家打出的卡牌类型强制改变意图。
    /// </summary>
    public void ReactToCard(CardType type)
    {
        var rng = Creature.CombatState.RunState.Rng.MonsterAi;
        MoveState nextState;

        if (type == CardType.Attack) nextState = _attackState;
        else if (type == CardType.Skill) nextState = _defendState;
        else if (type == CardType.Power) nextState = _buffState;
        else nextState = _allStates[rng.NextInt(_allStates.Length)]; // 其他类型随机

        // 强制把当前意图切换为对应的状态
        SetMoveImmediate(nextState, true);
    }

    private async Task AttackMove(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(Damage).FromMonster(this)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);
    }

    private async Task DefendMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, Block, ValueProp.Move, null);
    }

    private async Task BuffMove(IReadOnlyList<Creature> targets)
    {
        var ctx = new ThrowingPlayerChoiceContext();
        if (StrengthAmount > 0)
            await PowerCmd.Apply<StrengthPower>(ctx, Creature, StrengthAmount, Creature, null);
        if (DexterityAmount > 0)
            await PowerCmd.Apply<DexterityPower>(ctx, Creature, DexterityAmount, Creature, null);
    }
}