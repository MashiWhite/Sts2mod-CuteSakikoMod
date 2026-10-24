using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Nodes; // ★ 引入你的 VfxUtil
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Weak;

[RegisterMonster]
public class Xiaoyin : ModMonsterTemplate
{
    // ★ 定义特效场景路径，方便在 Entry.cs 里预加载
    public const string BunVfxPath = "res://CuteSakikoMod/scenes/vfx/bun.tscn";

    public override int MinInitialHp =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 98, 90);

    public override int MaxInitialHp =>
        AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 100, 92);

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/xiaoyin.tscn"
    );

    private int Damage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 6, 5);
    private decimal HealPercent => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 0.15m, 0.10m);
    private int StrengthAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 3, 2);

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
        => RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var move1 = new MoveState("THROW_BUN", ThrowBunMove, new MultiAttackIntent(Damage, 2));
        var move2 = new MoveState("EAT_BUN", EatBunMove, new BuffIntent(), new HealIntent());

        move1.FollowUpState = move2;
        move2.FollowUpState = move1;

        var states = new List<MonsterState> { move1, move2 };
        return new MonsterMoveStateMachine(states, move1);
    }

    private async Task ThrowBunMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Attack", 0.3f);

        var tasks = new List<Task>();
        
        foreach (var target in targets)
        {
            if (target.IsDead) continue;
            
            // 每名玩家丢2次馒头，稍微错开一点时间
            tasks.Add(ThrowBunAtTarget(target, 0.0f));
            tasks.Add(ThrowBunAtTarget(target, 0.15f));
        }
        
        await Task.WhenAll(tasks);
    }

        private async Task ThrowBunAtTarget(Creature target, float delay)
    {
        if (target.IsDead) return;
        if (delay > 0) await Cmd.Wait(delay);
        if (target.IsDead) return;

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

        // === 抛物线动画 ===
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

        // === 砸中：脆脆的碎裂感 ===
        if (GodotObject.IsInstanceValid(bun))
        {
            // 1. 屏幕微震（增加撞击的厚重感）
            NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
            
            // 2. 播放清脆的碎裂音效
            SfxCmd.Play("event:/sfx/block_hit"); 

            // 3. 主体馒头瞬间压扁并消失
            var breakTween = bun.CreateTween();
            breakTween.SetParallel();
            breakTween.TweenProperty(bun, "scale", new Vector2(2.5f, 0.5f), 0.05f); // 瞬间压扁
            breakTween.TweenProperty(bun, "modulate:a", 0.0f, 0.08f); // 瞬间透明
            breakTween.Chain().TweenCallback(Callable.From(() => bun.QueueFreeSafely()));

            // 4. ★ 炸出 4 个向四周弹射的小碎屑
            for (int i = 0; i < 4; i++)
            {
                var crumb = VfxUtil.GenVFXNode(BunVfxPath);
                parent.AddChildSafely(crumb);
                crumb.GlobalPosition = endPos;
                crumb.Scale = new Vector2(0.4f, 0.4f); // 碎屑比较小

                var crumbTween = crumb.CreateTween().SetParallel();
                // 随机向四周弹射的坐标
                Vector2 scatterDir = new Vector2((float)GD.RandRange(-120, 120), (float)GD.RandRange(-180, -60));
                crumbTween.TweenProperty(crumb, "position", endPos + scatterDir, 0.4f)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
                crumbTween.TweenProperty(crumb, "rotation", (float)GD.RandRange(-10, 10), 0.4f);
                // 后半段淡出
                crumbTween.TweenProperty(crumb, "modulate:a", 0.0f, 0.2f).SetDelay(0.2f);
                
                crumbTween.Chain().TweenCallback(Callable.From(() => crumb.QueueFreeSafely()));
            }
        }

        // ★ 5. 造成伤害（补全了第 7 个参数 CardPlay? cardPlay 传 null）
        await CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            target,
            Damage,
            ValueProp.Move,
            Creature, // dealer
            null,     // cardSource
            null      // cardPlay
        );
    }

    private async Task EatBunMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Cast", 0.5f);

        int lostHp = Creature.MaxHp - Creature.CurrentHp;
        int healAmount = (int)Math.Ceiling(lostHp * HealPercent);
        if (healAmount > 0)
        {
            await CreatureCmd.Heal(Creature, healAmount);
        }

        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), Creature, StrengthAmount, Creature, null);
    }
}