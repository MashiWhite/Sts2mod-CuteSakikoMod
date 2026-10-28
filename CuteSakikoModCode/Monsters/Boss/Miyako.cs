using CuteSakikoMod.CuteSakikoModCode.Nodes.Vfx;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
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
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;
using STS2RitsuLib.Scaffolding.Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Monsters.Boss;

/// <summary>
///     藤都子（第 2 幕 Boss）。开局带手偶丸君，会随手把月饼/壶/鸭子扔过来。
///     男子禁止：按玩家性别改变她受到的伤害与造成的伤害（见 MiyakoNoBoysPower）。
///     最好的朋友：丸君在场回血，丸君死后每回合 +3 力量（见 MiyakoBestFriendPower）。
/// </summary>
[RegisterMonster]
public class Miyako : ModMonsterTemplate
{
    public const string ThrowVfxPath = "res://CuteSakikoMod/scenes/vfx/miyako_throw.tscn";

    public override int MinInitialHp => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 352, 320);
    public override int MaxInitialHp => MinInitialHp;

    public override MonsterAssetProfile AssetProfile => new(
        "res://CuteSakikoMod/scenes/monster/miyako.tscn"
    );

    private int AttackDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 14, 12);
    private int ScoldDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);
    private int RageDamage => AscensionHelper.GetValueIfAscension(AscensionLevel.DeadlyEnemies, 9, 8);
    private int BlockAmount => AscensionHelper.GetValueIfAscension(AscensionLevel.ToughEnemies, 12, 10);

    private const int VulnerableAmount = 2;
    private const int StrengthAmount = 2;

    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        return RitsuGodotNodeFactories.CreateFromScenePath<NCreatureVisuals>(AssetProfile.VisualsScenePath!);
    }

    public override async Task AfterAddedToRoom()
    {
        await base.AfterAddedToRoom();
        var ctx = new ThrowingPlayerChoiceContext();
        await PowerCmd.Apply<MiyakoNoBoysPower>(ctx, Creature, 1, Creature, null);
        await PowerCmd.Apply<MiyakoBestFriendPower>(ctx, Creature, 1, Creature, null);
    }

    private bool MaruAlive =>
        Creature.CombatState?.Enemies.Any(e => e.IsAlive && e.Monster is MaruKun) ?? false;

    protected override MonsterMoveStateMachine GenerateMoveStateMachine()
    {
        var throwSomething = new MoveState("THROW_SOMETHING", ThrowSomethingMove,
            new SingleAttackIntent(AttackDamage));
        var scold = new MoveState("SCOLD", ScoldMove,
            new SingleAttackIntent(ScoldDamage), new DebuffIntent());
        var puppetShow = new MoveState("PUPPET_SHOW", PuppetShowMove,
            new DefendIntent(), new BuffIntent());
        var rage = new MoveState("RAGE", RageMove,
            new MultiAttackIntent(RageDamage, 3));

        // 丸君还活着 → 正常循环；丸君死了 → 下一招变成迁怒
        var maruCheck = new ConditionalBranchState("MARU_CHECK");
        maruCheck.AddState(rage, () => !MaruAlive);
        maruCheck.AddState(puppetShow, () => true);

        throwSomething.FollowUpState = scold;
        scold.FollowUpState = maruCheck;
        puppetShow.FollowUpState = throwSomething;
        rage.FollowUpState = scold;

        var states = new List<MonsterState>
        {
            throwSomething, scold, puppetShow, rage, maruCheck
        };
        return new MonsterMoveStateMachine(states, throwSomething);
    }

    private async Task ThrowSomethingMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Attack", 0.3f);

        foreach (var target in AliveTargets(targets))
            await ThrowAtTarget(target, AttackDamage);
    }

    private async Task ScoldMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Attack", 0.3f);

        foreach (var target in AliveTargets(targets))
            await ThrowAtTarget(target, ScoldDamage);

        var state = Creature.CombatState;
        if (state == null) return;

        foreach (var player in state.Players)
            await PowerCmd.Apply<VulnerablePower>(
                new ThrowingPlayerChoiceContext(), player.Creature, VulnerableAmount, Creature, null);
    }

    private async Task PuppetShowMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.GainBlock(Creature, BlockAmount, ValueProp.Move, null);
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), Creature, StrengthAmount, Creature, null);
    }

    private async Task RageMove(IReadOnlyList<Creature> targets)
    {
        await CreatureCmd.TriggerAnim(Creature, "Attack", 0.3f);

        // 3 连击：三样东西几乎是"一股脑"全丢出去，每发只差一点点起手时间
        var alive = AliveTargets(targets).ToList();
        var tasks = new List<Task>();
        for (var hit = 0; hit < 3; hit++)
        {
            var delay = hit * 0.09f;
            foreach (var target in alive)
                tasks.Add(ThrowAtTarget(target, RageDamage, delay));
        }

        await Task.WhenAll(tasks);
    }

    private IEnumerable<Creature> AliveTargets(IReadOnlyList<Creature> targets)
    {
        var list = targets.Count > 0
            ? targets
            : Creature.CombatState?.Players.Select(p => p.Creature).ToList() ?? [];

        return list.Where(t => t is { IsDead: false });
    }

    /// <summary>丸君被打败时，藤都子喊一声（参考 QUEEN 的 amalgamDeathSpeakLine）。</summary>
    public override Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (creature.Monster is MaruKun && Creature.IsAlive)
        {
            var line = MonsterModel.L10NMonsterLookup(Id.Entry + ".maruDeathSpeakLine");
            TalkCmd.Play(line, Creature, VfxColor.Purple);
        }

        return Task.CompletedTask;
    }

    /// <summary>把随机的一样东西（月饼/壶/鸭子）抛向目标，落点砸碎并结算伤害。</summary>
    private async Task ThrowAtTarget(Creature target, int damage, float delaySeconds = 0f)
    {
        if (delaySeconds > 0f)
        {
            var roomNode = NCombatRoom.Instance;
            var timer = roomNode?.GetTree().CreateTimer(delaySeconds);
            if (roomNode != null && timer != null)
                await roomNode.ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
        }

        var room = NCombatRoom.Instance;
        var monsterNode = room?.GetCreatureNode(Creature);
        var targetNode = room?.GetCreatureNode(target);
        if (room == null || monsterNode == null || targetNode == null)
        {
            // 表现层拿不到节点时也要保证伤害结算
            await DealDamage(target, damage);
            return;
        }

        var startPos = monsterNode.VfxSpawnPosition;
        var endPos = targetNode.Hitbox.GlobalPosition + targetNode.Hitbox.Size / 2f;

        var throwNode = VfxUtil.GenVFXNode(ThrowVfxPath);
        room.CombatVfxContainer.AddChildSafely(throwNode);
        throwNode.GlobalPosition = startPos;
        throwNode.Scale = new Vector2(1.2f, 1.2f);

        const float duration = 0.45f;
        var controlPoint = (startPos + endPos) / 2f;
        controlPoint.Y -= 300f;

        var tween = throwNode.CreateTween();
        tween.TweenMethod(Callable.From<float>(t =>
        {
            if (!GodotObject.IsInstanceValid(throwNode)) return;
            var u = 1 - t;
            throwNode.GlobalPosition = u * u * startPos + 2 * u * t * controlPoint + t * t * endPos;
            throwNode.Rotation = t * Mathf.Pi * 4f;
        }), 0f, 1f, duration).SetTrans(Tween.TransitionType.Linear);

        await tween.AwaitFinished(throwNode);

        if (GodotObject.IsInstanceValid(throwNode))
        {
            NGame.Instance?.ScreenShake(ShakeStrength.Weak, ShakeDuration.Short);
            SfxCmd.Play("event:/sfx/block_hit");

            // 砸碎：本体压扁淡出
            var breakTween = throwNode.CreateTween();
            breakTween.SetParallel();
            breakTween.TweenProperty(throwNode, "scale", new Vector2(1.6f, 0.4f), 0.05f);
            breakTween.TweenProperty(throwNode, "modulate", new Color(1, 1, 1, 0), 0.08f);
            breakTween.Chain().TweenCallback(Callable.From(() => throwNode.QueueFreeSafely()));

            // 碎片：用和本体完全相同的贴图，别再随机一次
            var pickedTexture = (throwNode as MiyakoThrowVfx)?.PickedTexture;
            for (var i = 0; i < 4; i++)
            {
                Node2D crumb;
                if (pickedTexture != null)
                {
                    crumb = new Sprite2D { Texture = pickedTexture };
                    room.CombatVfxContainer.AddChildSafely(crumb);
                    crumb.GlobalPosition = endPos;
                    crumb.Scale = new Vector2(0.4f, 0.4f);
                }
                else
                {
                    crumb = VfxUtil.GenVFXNode(ThrowVfxPath);
                    room.CombatVfxContainer.AddChildSafely(crumb);
                    crumb.GlobalPosition = endPos;
                    crumb.Scale = new Vector2(0.4f, 0.4f);
                }

                var crumbTween = crumb.CreateTween().SetParallel();
                var scatter = new Vector2((float)GD.RandRange(-120, 120), (float)GD.RandRange(-180, -60));
                crumbTween.TweenProperty(crumb, "position", endPos + scatter, 0.4f)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
                crumbTween.TweenProperty(crumb, "rotation", (float)GD.RandRange(-10, 10), 0.4f);
                crumbTween.TweenProperty(crumb, "modulate", new Color(1, 1, 1, 0), 0.2f).SetDelay(0.2f);
                crumbTween.Chain().TweenCallback(Callable.From(() => crumb.QueueFreeSafely()));
            }
        }

        await DealDamage(target, damage);
    }

    private Task DealDamage(Creature target, int damage)
    {
        return CreatureCmd.Damage(
            new ThrowingPlayerChoiceContext(),
            target,
            damage,
            ValueProp.Move,
            Creature,
            null,
            null);
    }
}
