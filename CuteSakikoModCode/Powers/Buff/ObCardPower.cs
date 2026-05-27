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

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;
    public override bool AllowNegative => false;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Sakiforget);
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Memory);
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
            MemoryCmd.Forget(choiceContext, new[] { card });
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

    // ────────── 视觉替换方法 ──────────
    private async Task ReplaceVisual()
    {
        // 1. 清理旧 Ob 节点（如果有）
        if (_obVisual != null)
        {
            _obVisual.QueueFree();
            _obVisual = null;
        }

        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (creatureNode == null) return;

        // 2. 隐藏原始模型
        var originalVisual = creatureNode.GetChild<NCreatureVisuals>(0);
        if (originalVisual != null)
        {
            originalVisual.Visible = false;
            originalVisual.Modulate = new Color(1, 1, 1, 0);
        }

        // 3. 加载 OB 场景并添加
        var scene = GD.Load<PackedScene>(ObScenePath);
        if (scene == null) return;
        _obVisual = scene.Instantiate<Node2D>();
        _obVisual.Name = "ObVisual"; // 起个名字，方便识别
        creatureNode.AddChild(_obVisual);

        // 4. 获取 AnimationPlayer
        var animPlayer = _obVisual.GetNode<AnimationPlayer>("Visuals/Node2D/AnimationPlayer");
        if (animPlayer != null)
        {
            ObAnimPlayers[Owner] = animPlayer;
        }
        else
        {
            GD.PushError("ObCardPower: 未找到 AnimationPlayer，请检查 ob.tscn 路径是否正确");
        }

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

        // 恢复原始模型
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner);
        if (creatureNode != null)
        {
            var originalVisual = creatureNode.GetChild<NCreatureVisuals>(0);
            if (originalVisual != null)
            {
                originalVisual.Visible = true;
                originalVisual.Modulate = new Color(1, 1, 1);
            }
        }

        await Task.CompletedTask;
    }
}