using CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Status;
using Godot;
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
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;
using System.Linq;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Boss.ChocolateSnail;

[RegisterMonster]
public class GiantChocolateSnail : ModMonsterTemplate
{
    private const int MaxSnails = 5;

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

        var afterBlockBranch = new ConditionalBranchState("AFTER_BLOCK_BRANCH");
        afterBlockBranch.AddState(_summonState, () => CountAliveSnails() < 3);
        afterBlockBranch.AddState(strengthState, () => CountAliveSnails() >= 3);

        _summonState.FollowUpState = blockState;
        blockState.FollowUpState = afterBlockBranch;
        strengthState.FollowUpState = cardState;
        cardState.FollowUpState = blockState;

        var states = new List<MonsterState>
        {
            _summonState, blockState, strengthState, cardState, afterBlockBranch
        };
        return new MonsterMoveStateMachine(states, _summonState);
    }

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != CombatSide.Enemy) return;
        if (CountAliveSnails() == 0)
        {
            SetMoveImmediate(_summonState, true);
        }
    }

    private int CountAliveSnails()
    {
        return Creature.CombatState.GetTeammatesOf(Creature)
            .Count(c => c.IsAlive && c.Monster is SmallChocolateSnail);
    }

    private async Task SummonMove(IReadOnlyList<Creature> targets)
    {
        var combatState = Creature.CombatState;
        var encounter = combatState.Encounter;

        int needed = MaxSnails - CountAliveSnails();
        if (needed <= 0) return;

        // 过滤出可用的 snail 槽位
        var snailSlots = encounter.Slots
            .Where(s => s.StartsWith("snail"))
            .Where(s => !combatState.Enemies.Any(e => e.SlotName == s))
            .ToList();

        int summoned = 0;
        bool usedFallback = false;

        // 先用专属槽位
        foreach (var slot in snailSlots)
        {
            if (summoned >= needed) break;
            var snail = await CreatureCmd.Add<SmallChocolateSnail>(combatState, slot);
            await PowerCmd.Apply<MinionPower>(
                new ThrowingPlayerChoiceContext(), snail, 1, Creature, null);
            summoned++;
        }

        // 槽位不够或根本没有时，回退到无槽位召唤
        while (summoned < needed)
        {
            var snail = await CreatureCmd.Add<SmallChocolateSnail>(combatState);
            await PowerCmd.Apply<MinionPower>(
                new ThrowingPlayerChoiceContext(), snail, 1, Creature, null);
            summoned++;
            usedFallback = true;
        }

        // 只要用到了回退召唤，就重排一次，让它们竖着站
        if (usedFallback)
        {
            RepositionSlotlessSnailsNearBoss(combatState);
        }
    }
    
    /// <summary>
    /// 把无槽位的小巧克力螺紧贴大螺左侧，按「两行交错」方式排列。
    /// 索引 0、2、4 … 在上行，1、3、5 … 在下行；
    /// 索引 0 最靠近大螺，之后的索引依次向左推。
    /// </summary>
    private void RepositionSlotlessSnailsNearBoss(ICombatState combatState)
    {
        var room = NCombatRoom.Instance;
        if (room == null) return;

        var bossNode = room.GetCreatureNode(Creature);
        if (bossNode == null) return;

        var nodes = room.CreatureNodes
            .Where(n => GodotObject.IsInstanceValid(n) && n.Entity != null
                                                       && n.Entity.Monster is SmallChocolateSnail
                                                       && string.IsNullOrEmpty(n.Entity.SlotName)
                                                       && !n.Entity.IsDead)
            .Take(64)
            .ToArray();

        if (nodes.Length == 0) return;

        // 每个小螺的最大视觉尺寸，用来确定间距
        float maxWidth = 0f, maxHeight = 0f;
        foreach (var node in nodes)
        {
            float w = node.Visuals?.Bounds.Size.X ?? 0;
            float h = node.Visuals?.Bounds.Size.Y ?? 0;
            maxWidth = Mathf.Max(maxWidth, w > 0 ? w : 100f);
            maxHeight = Mathf.Max(maxHeight, h > 0 ? h : 80f);
        }

        float xSpacing = maxWidth + 15f;   // 横向相邻两列的间距
        float ySpacing = maxHeight + 10f;  // 上下两行的间距

        // 大螺左边缘往左挪一点，就是索引 0 的中心位置
        float bossHalfWidth = (bossNode.Visuals?.Bounds.Size.X ?? 120f) * 0.5f;
        float baseX = bossNode.Position.X - bossHalfWidth - maxWidth * 0.5f - 20f;
        float baseY = bossNode.Position.Y;

        for (int i = 0; i < nodes.Length; i++)
        {
            int row = i % 2;   // 0 = 上行，1 = 下行
            int col = i / 2;   // 0 = 最右（紧贴大螺），1 = 往左一列 …

            float x = baseX - col * xSpacing - (row == 1 ? xSpacing * 0.5f : 0f);
            float y = baseY + (row - 0.5f) * ySpacing;

            nodes[i].Position = new Vector2(x, y);
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