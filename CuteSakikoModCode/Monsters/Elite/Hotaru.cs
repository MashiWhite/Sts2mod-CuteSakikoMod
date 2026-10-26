using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs;
using CuteSakikoMod.CuteSakikoModCode.Others; // ★ 引入 VfxUtil
using Godot; // ★ 需要用到 Tween 和 Vector2
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Elite;

[RegisterMonster]
public class Hotaru : ModMonsterTemplate
{
    // 复用同一个馒头特效路径
    public const string BunVfxPath = "res://CuteSakikoMod/scenes/vfx/bun.tscn";

    public override int MinInitialHp =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 185, 175);

    public override int MaxInitialHp =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 200, 190);

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/hotaru.tscn"
    );

    private int BlockTimes =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    private int Damage =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 19, 17);

    private int StrengthAmount =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 4, 3);

    private int PolluteCount =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

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
        
        // ★ 改回单次攻击意图
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

    // ───── 意图 1：获得格挡 ×N ─────
    private async Task Move1Block(IReadOnlyList<Creature> targets)
    {
        for (int i = 0; i < BlockTimes; i++)
            await CreatureCmd.GainBlock(Creature, 5, ValueProp.Move, null);
    }

    // ───── 意图 2：丢馒头造成伤害 + 获得敏捷 ─────
    private async Task Move2Attack(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Attack", 0.3f);

        var tasks = new List<Task>();
        foreach (var target in targets)
        {
            if (target.IsDead) continue;
            tasks.Add(ThrowBunAtTarget(target));
        }
        
        // 等待馒头落地并结算完伤害
        await Task.WhenAll(tasks);

        // 获得敏捷
        await PowerCmd.Apply<DexterityPower>(
            new ThrowingPlayerChoiceContext(), Creature, 2, Creature, null);
    }

    // ★ 直接复用你写好的小馒头抛物线逻辑
    private async Task ThrowBunAtTarget(Creature target)
    {
        var monsterNode = NCombatRoom.Instance?.GetCreatureNode(Creature);
        var targetNode = NCombatRoom.Instance?.GetCreatureNode(target);
        if (monsterNode == null || targetNode == null) return;

        Vector2 startPos = monsterNode.VfxSpawnPosition;
        Vector2 endPos = targetNode.Hitbox.GlobalPosition + (targetNode.Hitbox.Size / 2f);

        var bun = VfxUtil.GenVFXNode(BunVfxPath);
        var parent = NCombatRoom.Instance.CombatVfxContainer;
        parent.AddChildSafely(bun);
        bun.GlobalPosition = startPos;
        bun.Scale = new Vector2(1.5f, 1.5f);

        float duration = 0.4f; 
        Vector2 controlPoint = (startPos + endPos) / 2f;
        controlPoint.Y -= 300f; 

        var tween = bun.CreateTween();
        tween.TweenMethod(Callable.From<float>(t =>
        {
            if (GodotObject.IsInstanceValid(bun))
            {
                float u = 1 - t;
                Vector2 pos = (u * u * startPos) + (2 * u * t * controlPoint) + (t * t * endPos);
                bun.GlobalPosition = pos;
                bun.Rotation = t * Mathf.Pi * 4f;
            }
        }), 0.0f, 1.0f, duration).SetTrans(Tween.TransitionType.Linear);

        await tween.AwaitFinished(bun);

        if (GodotObject.IsInstanceValid(bun))
        {
            NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
            SfxCmd.Play("event:/sfx/block_hit"); 

            var breakTween = bun.CreateTween();
            breakTween.SetParallel();
            breakTween.TweenProperty(bun, "scale", new Vector2(2.5f, 0.5f), 0.05f); 
            breakTween.TweenProperty(bun, "modulate", new Color(1, 1, 1, 0), 0.08f); 
            breakTween.Chain().TweenCallback(Callable.From(() => bun.QueueFreeSafely()));

            for (int i = 0; i < 4; i++)
            {
                var crumb = VfxUtil.GenVFXNode(BunVfxPath);
                parent.AddChildSafely(crumb);
                crumb.GlobalPosition = endPos;
                crumb.Scale = new Vector2(0.4f, 0.4f);
                crumb.Modulate = new Color(1, 1, 1, 1);

                var crumbTween = crumb.CreateTween().SetParallel();
                Vector2 scatterDir = new Vector2((float)GD.RandRange(-120, 120), (float)GD.RandRange(-180, -60));
                crumbTween.TweenProperty(crumb, "position", endPos + scatterDir, 0.4f)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
                crumbTween.TweenProperty(crumb, "rotation", (float)GD.RandRange(-10, 10), 0.4f);
                crumbTween.TweenProperty(crumb, "modulate", new Color(1, 1, 1, 0), 0.2f).SetDelay(0.2f);
                
                crumbTween.Chain().TweenCallback(Callable.From(() => crumb.QueueFreeSafely()));
            }
        }

        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            target,
            Damage,
            ValueProp.Move,
            Creature,
            null,
            null
        );
    }
    
    // ───── 意图 3：获得力量 + 向玩家抽牌堆塞随机角色牌 ─────
    private async Task Move3BuffAndPollute(IReadOnlyList<Creature> targets)
    {
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), Creature, StrengthAmount, Creature, null);

        var addedResults = new List<CardPileAddResult>();

        foreach (var player in Creature.CombatState.Players)
        {
            for (int i = 0; i < PolluteCount; i++)
            {
                var card = PickRandomOtherCharacterCard(player);
                if (card == null) continue;

                var result = await CardPileCmd.AddGeneratedCardToCombat(
                    card, PileType.Draw, player, CardPilePosition.Random);
            
                addedResults.Add(result);
            }
        }

        if (addedResults.Count > 0)
        {
            CardCmd.PreviewCardPileAdd(addedResults);
            await Cmd.Wait(0.3f);
        }
    }

    private CardModel? PickRandomOtherCharacterCard(Player player)
    {
        var combatState = Creature.CombatState;
        if (combatState == null) return null;

        var myPool = player.Character.CardPool;
        var rng = combatState.RunState.Rng.CombatCardGeneration;

        var allCandidates = ModelDb.AllCards
            .Where(c => c.Pool != null 
                     && c.Pool != myPool 
                     && c.Rarity != CardRarity.Ancient 
                     && c.Rarity != CardRarity.Basic
                     && c is not CuteSakikoModEggCard
                     && c is not DeprecatedCard 
                     && c.ShouldShowInCardLibrary) 
            .ToList();

        if (allCandidates.Count == 0) return null;

        var commons = allCandidates.Where(c => c.Rarity == CardRarity.Common && c.Type != CardType.Status && c.Type != CardType.Curse).ToList();
        var uncommons = allCandidates.Where(c => c.Rarity == CardRarity.Uncommon && c.Type != CardType.Status && c.Type != CardType.Curse).ToList();
        var rares = allCandidates.Where(c => c.Rarity == CardRarity.Rare && c.Type != CardType.Status && c.Type != CardType.Curse).ToList();
        var statuses = allCandidates.Where(c => c.Type == CardType.Status).ToList();
        var curses = allCandidates.Where(c => c.Type == CardType.Curse).ToList();

        int weightCommon = commons.Count > 0 ? 45 : 0;
        int weightUncommon = uncommons.Count > 0 ? 25 : 0;
        int weightRare = rares.Count > 0 ? 15 : 0;
        int weightStatus = statuses.Count > 0 ? 10 : 0;
        int weightCurse = curses.Count > 0 ? 5 : 0;

        int totalWeight = weightCommon + weightUncommon + weightRare + weightStatus + weightCurse;

        if (totalWeight == 0) return null;

        int roll = rng.NextInt(totalWeight); 
        List<CardModel> targetList = new List<CardModel>();

        if (roll < weightCommon) targetList = commons;
        else if (roll < weightCommon + weightUncommon) targetList = uncommons;
        else if (roll < weightCommon + weightUncommon + weightRare) targetList = rares;
        else if (roll < weightCommon + weightUncommon + weightRare + weightStatus) targetList = statuses;
        else targetList = curses;

        var picked = targetList[rng.NextInt(targetList.Count)];
        return combatState.CreateCard(picked, player);
    }
}