using System.Collections;
using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class ObCardPower : CuteSakikoModPower
{
    private const int ExtraReplay = 1;

    // ────────── 视觉替换相关字段 ──────────
    internal static readonly Dictionary<Creature, AnimationPlayer> ObAnimPlayers = new();
    private static readonly string ObScenePath = "res://CuteSakikoMod/scenes/char/saki/ob.tscn";
    private readonly Dictionary<CardModel, bool> _hadExhaustKeyword = new();
    private readonly HashSet<CardModel> _modifiedCards = new();
    private readonly Dictionary<CardModel, int> _originalCosts = new();
    private bool _isRemoving;
    private Node2D? _obVisual;
    private NCreatureVisuals? _originalVisual;      // 缓存原始视觉节点，避免索引查找

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory.GetModCardKeyword());
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sakiforget.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    // ========== 应用时修改卡牌 + 替换视觉 ==========
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        await ModifyExistingCards();
        await ReplaceVisual();
    }

    // ========== 移除时恢复视觉并清理 ==========
    public override async Task AfterRemoved(Creature oldOwner)
    {
        await RestoreVisual();
        ObAnimPlayers.Remove(oldOwner);
        await base.AfterRemoved(oldOwner);
    }

    // ========== 以下为原有逻辑（无改动）==========
    private async Task ModifyExistingCards()
    {
        if (Owner?.Player == null) return;
        var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust };
        foreach (var pileType in piles)
        {
            var pile = pileType.GetPile(Owner.Player);
            if (pile == null) continue;
            foreach (var card in pile.Cards)
                if (!_modifiedCards.Contains(card))
                    ApplyModificationsToCard(card);
        }

        await Task.CompletedTask;
    }

    public override async Task AfterCardEnteredCombat(CardModel card)
    {
        if (_isRemoving) return;
        if (card.Owner != Owner.Player) return;
        if (!_modifiedCards.Contains(card))
            ApplyModificationsToCard(card);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (_isRemoving) return;
        var card = cardPlay.Card;
        if (card.Owner?.Creature != Owner) return;

        if (_modifiedCards.Contains(card))
        {
            await MemoryCmd.Forget(choiceContext, new[] { card });
            _modifiedCards.Remove(card);
            _originalCosts.Remove(card);
        }
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (_isRemoving) return;
        if (side != Owner.Side) return;

        var pressure = Owner.GetPower<PressurePower>();
        if (pressure == null || pressure.Amount == 0)
            await RemovePowerAndRestore();
    }

    private void ApplyModificationsToCard(CardModel card)
    {
        if (!_originalCosts.ContainsKey(card))
        {
            var originalCost = card.EnergyCost.GetWithModifiers(CostModifiers.None);
            _originalCosts[card] = originalCost;
        }

        card.EnergyCost.SetThisCombat(1, true);
        card.BaseReplayCount += ExtraReplay;
        _modifiedCards.Add(card);
    }

    private async Task RemovePowerAndRestore()
    {
        if (_isRemoving) return;
        _isRemoving = true;

        if (Owner?.Player != null)
        {
            var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust };
            foreach (var pileType in piles)
            {
                var pile = pileType.GetPile(Owner.Player);
                if (pile == null) continue;
                foreach (var card in pile.Cards)
                {
                    if (!_modifiedCards.Contains(card)) continue;
                    if (_originalCosts.TryGetValue(card, out var originalCost))
                    {
                        var energyCost = card.EnergyCost;
                        var modifiersField = energyCost.GetType()
                            .GetField("_localModifiers", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (modifiersField?.GetValue(energyCost) is IList list)
                            list.Clear();

                        var baseField = energyCost.GetType()
                            .GetField("_base", BindingFlags.NonPublic | BindingFlags.Instance);
                        if (baseField != null)
                            baseField.SetValue(energyCost, originalCost);

                        card.InvokeEnergyCostChanged();
                    }

                    card.BaseReplayCount -= ExtraReplay;
                    _modifiedCards.Remove(card);
                    _originalCosts.Remove(card);
                }
            }
        }

        _modifiedCards.Clear();
        _originalCosts.Clear();
        await PowerCmd.Remove(this);
    }

    // ────────── 视觉替换方法（修复版）──────────
    private async Task ReplaceVisual()
    {
        // 清理旧的 Ob 节点
        if (_obVisual != null)
        {
            _obVisual.QueueFree();
            _obVisual = null;
        }

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (creatureNode == null) return;

        _originalVisual = creatureNode.FindChildOfType<NCreatureVisuals>();
        if (_originalVisual != null)
        {
            _originalVisual.Visible = false;
            _originalVisual.Modulate = new Color(1, 1, 1, 0);
        }
        else
        {
            GD.PushError("ObCardPower: 未找到原始 NCreatureVisuals 节点");
        }

        var scene = GD.Load<PackedScene>(ObScenePath);
        if (scene == null) return;
        _obVisual = scene.Instantiate<Node2D>();
        _obVisual.Name = "ObVisual";

        // ★ 禁用新视觉中所有 Control 的鼠标交互，避免阻挡旧 Bounds
        foreach (var control in _obVisual.FindChildrenOfType<Control>())
        {
            control.MouseFilter = Control.MouseFilterEnum.Ignore;
        }

        creatureNode.AddChild(_obVisual);

        var animPlayer = _obVisual.GetNode<AnimationPlayer>("Visuals/Node2D/AnimationPlayer");
        if (animPlayer != null)
            ObAnimPlayers[Owner] = animPlayer;
        else
            GD.PushError("ObCardPower: 未找到 AnimationPlayer，请检查 ob.tscn 路径是否正确");

        await Task.CompletedTask;
    }

    private async Task RestoreVisual()
    {
        // 销毁 Ob 节点
        if (_obVisual != null)
        {
            _obVisual.QueueFree();
            _obVisual = null;
        }

        // 清理映射
        ObAnimPlayers.Remove(Owner);

        // 恢复原始模型（优先使用缓存的引用）
        if (_originalVisual != null && IsInstanceValid(_originalVisual))
        {
            _originalVisual.Visible = true;
            _originalVisual.Modulate = new Color(1, 1, 1);
        }
        else
        {
            // 降级方案：重新从 creatureNode 查找
            var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
            if (creatureNode != null)
            {
                var visual = creatureNode.FindChildOfType<NCreatureVisuals>();
                if (visual != null)
                {
                    visual.Visible = true;
                    visual.Modulate = new Color(1, 1, 1);
                }
            }
        }

        _originalVisual = null;
        await Task.CompletedTask;
    }

    // 辅助方法：检查节点是否有效（未被销毁且仍在场景树中）
    private static bool IsInstanceValid(Node node) => node != null && !node.IsQueuedForDeletion() && node.IsInsideTree();
}

