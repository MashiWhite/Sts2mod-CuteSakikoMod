using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class PigPower : CuteSakikoModPower
{
    // 存储生物 -> 动画播放器的映射
    internal static Dictionary<Creature, AnimationPlayer> PigAnimPlayers = new();

    private readonly PackedScene _pigScene;
    private Node2D? _pigVisual;

    public PigPower()
    {
        _pigScene = GD.Load<PackedScene>("res://CuteSakikoMod/scenes/char/Pig/pig.tscn");
    } 

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromCard<PigEat>(); }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;
        var pigEat = CombatState.CreateCard<PigEat>(player);
        await CardPileCmd.AddGeneratedCardToCombat(pigEat, PileType.Hand, player);
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        await ReplaceVisual();
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        await RestoreVisual();
        PigAnimPlayers.Remove(oldOwner);
        await base.AfterRemoved(oldOwner);
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
        if (side == Owner.Side && Owner != null && Owner.IsAlive) await CreatureCmd.Heal(Owner, 1);
    }

    public override bool ShouldDie(Creature creature)
    {
        if (creature != Owner) return base.ShouldDie(creature);
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner) return;
        var healAmount = Mathf.Max(1, (int)(creature.MaxHp * 0.1f));
        await CreatureCmd.Heal(creature, healAmount);
        await PowerCmd.Remove(this);
    }

    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner) return;
        ScalePig();
        await Task.CompletedTask;
    }

    private void ScalePig()
    {
        if (_pigVisual == null) return;
        // 根据当前生命值缩放根节点（外层 Node2D 已经隔离了偏移）
        const float minHp = 1f;
        const float maxHp = 1000f;
        const float minScale = 0.01f;
        const float maxScale = 10.0f;
        float t = Mathf.Clamp((Owner.CurrentHp - minHp) / (maxHp - minHp), 0f, 1f);
        float targetScale = Mathf.Lerp(minScale, maxScale, t);
        _pigVisual.Scale = Vector2.One * targetScale;
    }

    private async Task ReplaceVisual()
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (creatureNode == null) return;

        // 隐藏原有视觉
        var originalVisual = creatureNode.GetChild<NCreatureVisuals>(0);
        if (originalVisual != null) originalVisual.Visible = false;

        // 实例化猪场景
        _pigVisual = _pigScene.Instantiate<Node2D>();
        creatureNode.AddChild(_pigVisual);
        // 位置使用场景内部预设偏移，不额外重置

        // 直接通过固定路径获取 AnimationPlayer（场景结构已知）
        var animPlayer = _pigVisual.GetNode<AnimationPlayer>("Visuals/Node2D/AnimationPlayer");
        if (animPlayer != null)
        {
            PigAnimPlayers[Owner] = animPlayer;
        }
        else
        {
            GD.PushError("PigPower: 未找到 AnimationPlayer，请检查 pig.tscn 路径是否正确");
        }

        // 设置初始缩放
        ScalePig();
        await Task.CompletedTask;
    }

    private async Task RestoreVisual()
    {
        if (_pigVisual != null)
        {
            _pigVisual.QueueFree();
            _pigVisual = null;
        }

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (creatureNode != null)
        {
            var originalVisual = creatureNode.GetChild<NCreatureVisuals>(0);
            if (originalVisual != null) originalVisual.Visible = true;
        }

        await Task.CompletedTask;
    }
}