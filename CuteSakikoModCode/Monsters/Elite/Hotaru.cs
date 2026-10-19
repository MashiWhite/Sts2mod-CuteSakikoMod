using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
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

[RegisterMonster]
public class Hotaru : ModMonsterTemplate
{
    public override int MinInitialHp =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 175, 165);

    public override int MaxInitialHp =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 185, 175);

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/hotaru.tscn"
    );

    private int BlockTimes =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    private int Damage =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 17, 15);

    private int StrengthAmount =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    private int PolluteCount =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 2, 1);

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
        => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        var ctx = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<RainbowWorldPower>(ctx, Creature, 1, Creature, null);
    }

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        // 意图循环：1 → 2 → 3 → 1 → 2 → 3 ...
        var move1 = new MoveState("BLOCK_REPEAT", Move1Block, new DefendIntent());
        var move2 = new MoveState("ATTACK_DEX", Move2Attack,
            new SingleAttackIntent(Damage), new BuffIntent());
        var move3 = new MoveState("BUFF_AND_POLLUTE", Move3BuffAndPollute,
            new BuffIntent(), new StatusIntent(PolluteCount));

        move1.FollowUpState = move2;
        move2.FollowUpState = move3;
        move3.FollowUpState = move1;

        var states = new List<MonsterState> { move1, move2, move3 };
        return new MonsterMoveStateMachine(states, move1);
    }

    // ───── 意图 1：获得 2 点格挡 ×N ─────
    private async Task Move1Block(IReadOnlyList<Creature> targets)
    {
        for (int i = 0; i < BlockTimes; i++)
            await CreatureCmd.GainBlock(Creature, 2, ValueProp.Move, null);
    }

    // ───── 意图 2：造成伤害 + 获得 1 点敏捷 ─────
    private async Task Move2Attack(IReadOnlyList<Creature> targets)
    {
        await DamageCmd.Attack(Damage).FromMonster(this)
            .WithAttackerAnim("Attack", 0.3f)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(null);

        await PowerCmd.Apply<DexterityPower>(
            new ThrowingPlayerChoiceContext(), Creature, 1, Creature, null);
    }

    // ───── 意图 3：获得力量 + 向玩家抽牌堆塞随机角色牌 ─────
    private async Task Move3BuffAndPollute(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), Creature, StrengthAmount, Creature, null);

        foreach (var player in Creature.CombatState.Players)
        {
            for (int i = 0; i < PolluteCount; i++)
            {
                var card = PickRandomOtherCharacterCard(player);
                if (card == null) continue;

                await CardPileCmd.AddGeneratedCardToCombat(
                    card, PileType.Draw, player, CardPilePosition.Random);
            }
        }
    }

    private CardModel? PickRandomOtherCharacterCard(Player player)
    {
        var combatState = Creature.CombatState;
        if (combatState == null) return null;

        var myPool = player.Character.CardPool;

        // ★ ModelDb 遍历入口名字同上，请按项目实际调整
        var candidates = ModelDb.AllCards
            .Where(c => c.Pool != null
                     && c.Pool != myPool
                     && c.Type != CardType.Status
                     && c.Type != CardType.Curse
                     && c.Type != CardType.Skill   // 如不想塞技能可去掉这一行
                     && c.Rarity != CardRarity.Basic)
            .ToList();

        if (candidates.Count == 0) return null;

        var rng = combatState.RunState.Rng.CombatCardGeneration;
        var picked = candidates[rng.NextInt(candidates.Count)];
        return combatState.CreateCard(picked, player);
    }
}