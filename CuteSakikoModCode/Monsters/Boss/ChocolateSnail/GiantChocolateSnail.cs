using CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Status;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
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

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Boss.ChocolateSnail;

[RegisterMonster]
public class GiantChocolateSnail : ModMonsterTemplate
{
    private MoveState _summonState = null!;

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 170, 155);
    public override int MaxInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 180, 165);

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/giant_chocolate_snail.tscn"
    );

    private int BlockAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 30, 20);
    private int StrengthAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        _summonState = new MoveState("SUMMON_SNAILS", SummonMove, new SummonIntent());
        var blockState = new MoveState("BLOCK", BlockMove, new DefendIntent());
        var strengthState = new MoveState("BUFF_SNAILS", StrengthMove, new BuffIntent());
        var cardState = new MoveState("ADD_CARD", AddCardMove, new StatusIntent(1));

        // 循环：召唤 → 格挡 → 强化小怪 → 加卡 → 格挡 → 强化小怪 → 加卡 → ...
        _summonState.FollowUpState = blockState;
        blockState.FollowUpState = strengthState;
        strengthState.FollowUpState = cardState;
        cardState.FollowUpState = blockState;

        var states = new List<MonsterState> { _summonState, blockState, strengthState, cardState };
        return new MonsterMoveStateMachine(states, _summonState);
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Enemy) return;
        // 当场上所有小巧克力螺死亡时，强制回到召唤意图
        if (!HasAliveSnails())
        {
            SetMoveImmediate(_summonState, true);
        }
    }
    

    private bool HasAliveSnails()
    {
        return Creature.CombatState.GetTeammatesOf(Creature)
            .Any(c => c.IsAlive && c.Monster is SmallChocolateSnail);
    }

    private async Task SummonMove(IReadOnlyList<Creature> targets)
    {
        var combatState = Creature.CombatState;
        var encounter = combatState.Encounter;
        int summoned = 0;

        foreach (var slot in encounter.Slots)
        {
            if (summoned >= 3) break;
            if (!slot.StartsWith("snail")) continue;
            if (combatState.Enemies.Any(e => e.SlotName == slot)) continue;

            var snail = await CreatureCmd.Add<SmallChocolateSnail>(combatState, slot);
            await PowerCmd.Apply<MinionPower>(
                new ThrowingPlayerChoiceContext(), snail, 1, Creature, null);
            summoned++;
        }
    }

    private async Task BlockMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, BlockAmount, ValueProp.Move, null);
    }

    private async Task StrengthMove(IReadOnlyList<Creature> targets)
    {
        var snails = Creature.CombatState.GetTeammatesOf(Creature)
            .Where(c => c.IsAlive && c.Monster is SmallChocolateSnail)
            .ToList();

        foreach (var snail in snails)
        {
            await PowerCmd.Apply<StrengthPower>(
                new ThrowingPlayerChoiceContext(), snail, StrengthAmount, Creature, null);
        }
    }

    private async Task AddCardMove(IReadOnlyList<Creature> targets)
    {
        foreach (var player in Creature.CombatState.Players)
        {
            var card = Creature.CombatState.CreateCard<ChocolateSnailCard>(player);
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, player);
        }
    }
}