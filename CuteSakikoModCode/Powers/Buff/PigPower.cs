using BaseLib.Abstracts;
using BaseLib.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
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

public sealed class PigPower : CustomPowerModel
{
    // 存储生物 -> 动画播放器的映射
    internal static Dictionary<Creature, AnimationPlayer> PigAnimPlayers = new();

    private readonly PackedScene _pigScene;
    private Node2D? _pigVisual;

    public PigPower()
    {
        _pigScene = GD.Load<PackedScene>("res://CuteSakikoMod/scenes/others/pig.tscn");
    }

    public override string CustomPackedIconPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").PowerImagePath();

    public override string CustomBigIconPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").PowerImagePath();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromCard<PigEat>(); }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;
        // 每回合开始将一张猪进食加入手牌
        var pigEat = CombatState.CreateCard<PigEat>(player);

        await CardPileCmd.AddGeneratedCardToCombat(pigEat, PileType.Hand, true);
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

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        await base.AfterTurnEnd(choiceContext, side);
        if (side == Owner.Side && Owner != null && Owner.IsAlive) await CreatureCmd.Heal(Owner, 1);
    }

    // 当拥有者将要死亡时，阻止死亡
    public override bool ShouldDie(Creature creature)
    {
        if (creature != Owner) return base.ShouldDie(creature);
        // 拥有此能力，阻止死亡
        return false;
    }

    // 阻止死亡后，回复30%最大生命值，然后移除本能力
    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner) return;
        // 回复10%最大生命值（至少1点）
        var healAmount = Mathf.Max(1, (int)(creature.MaxHp * 0.1f));
        await CreatureCmd.Heal(creature, healAmount);
        // 移除本能力
        await PowerCmd.Remove(this);
    }

    // 生命值变化时更新缩放
    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner) return;
        ScalePig();
        await Task.CompletedTask;
    }

    private void ScalePig()
    {
        if (_pigVisual == null) return;
        // 根据当前生命值缩放：当前生命值低时缩小，高时放大
        const float minHp = 1f; // 最小生命值（缩放最小值）
        const float maxHp = 1000f; // 最大生命值（缩放最大值）
        const float minScale = 0.01f; // 最小缩放倍数
        const float maxScale = 10.0f; // 最大缩放倍数
        var t = Mathf.Clamp((Owner.CurrentHp - minHp) / (maxHp - minHp), 0f, 1f);
        var targetScale = Mathf.Lerp(minScale, maxScale, t);
        _pigVisual.Scale = Vector2.One * targetScale;
    }

    private async Task ReplaceVisual()
    {
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (creatureNode == null) return;

        // 隐藏原有视觉
        var originalVisual = creatureNode.GetChild<NCreatureVisuals>(0);
        if (originalVisual != null) originalVisual.Visible = false;

        // 实例化猪的视觉
        _pigVisual = _pigScene.Instantiate<Node2D>();
        creatureNode.AddChild(_pigVisual);
        _pigVisual.Position = Vector2.Zero;

        // 设置初始缩放
        ScalePig();

        // 查找猪的 AnimationPlayer 并存储
        var animPlayer = FindAnimationPlayer(_pigVisual);
        if (animPlayer != null)
            PigAnimPlayers[Owner] = animPlayer;
    }

    private async Task RestoreVisual()
    {
        if (_pigVisual != null)
        {
            _pigVisual.QueueFree();
            _pigVisual = null;
        }

        // 恢复原有视觉（假设只有一个子节点被隐藏）
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (creatureNode != null)
        {
            var originalVisual = creatureNode.GetChild<NCreatureVisuals>(0);
            if (originalVisual != null) originalVisual.Visible = true;
        }

        await Task.CompletedTask;
    }

    private AnimationPlayer FindAnimationPlayer(Node node)
    {
        if (node is AnimationPlayer ap) return ap;
        foreach (var child in node.GetChildren())
        {
            var found = FindAnimationPlayer(child);
            if (found != null) return found;
        }

        return null;
    }
}