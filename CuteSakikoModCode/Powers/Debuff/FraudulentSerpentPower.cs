using CuteSakikoMod.CuteSakikoModCode.Others.Afflictions;
using CuteSakikoMod.CuteSakikoModCode.Patches;
using CuteSakikoMod.CuteSakikoModCode.Patches;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using Timer = Godot.Timer;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;

/// <summary>
///     欺诈之蛇：你每回合抽到的前 Amount 张牌无法辨认。
///     被侵蚀的牌：卡图换成 viola、标题与描述变成持续跳动的乱码（见 Patches/ViolaErosionPatches）。
///     侵蚀会一直保留（打得越久看不清的牌越多），**只有打出才恢复**；power 被移除时兜底清理。
/// </summary>
public sealed class FraudulentSerpentPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    /// <summary>所有当前处于侵蚀状态的牌（跨回合累积，打出后移除）。</summary>
    private readonly HashSet<CardModel> _eroded = [];

    /// <summary>本回合已经侵蚀了几张（用于 Amount 配额）。</summary>
    private int _erodedThisTurn;

    private Timer? _ticker;

    /// <summary>
    ///     配额语义是"本回合抽的前 N 张"，所以按回合边界重置：回合结束时清零，下回合从 0 开始计。
    ///     注意两点（CombatManager.cs 已核对）：
    ///       · 不能用 AfterSideTurnStart —— 它在 SetupPlayerTurn(抽牌) 之后才触发，会导致回合开始抽的牌
    ///         按上回合的配额判定（这正是"抽牌卡能侵蚀、回合开始抽的反而没有"的原因）。
    ///       · 敌我双方的回合结束都会触发这个 hook，必须按 side 过滤。
    /// </summary>
    public override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side == Owner?.Side) _erodedThisTurn = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw)
    {
        var owner = Owner;
        if (owner?.Player == null) return;
        if (card.Owner != owner.Player) return;
        if (card.CombatState?.CurrentSide != owner.Side) return;

        // fromHandDraw 只代表"回合开始的初始抽牌"，回合中抽到的牌也算，所以不限制它
        if (_erodedThisTurn >= (int)Amount) return;

        if (!_eroded.Add(card)) return;

        await CardCmd.Afflict<ViolaErosion>(card, 1);
        _erodedThisTurn++;
        StartTicker();
        RefreshCard(card);
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (card == null || !_eroded.Contains(card)) return Task.CompletedTask;

        _eroded.Remove(card);
        CardCmd.ClearAffliction(card);
        RefreshCard(card);
        return Task.CompletedTask;
    }

    // 注意：回合结束时**不清除已侵蚀的牌**（那是设计），只重置本回合的配额计数（见 AfterSideTurnEnd）。

    public override Task AfterRemoved(Creature oldOwner)
    {
        ClearErosion();
        StopTicker();
        return Task.CompletedTask;
    }

    public void ClearErosion()
    {
        foreach (var card in _eroded)
        {
            if (card == null || card.Affliction is not ViolaErosion) continue;

            CardCmd.ClearAffliction(card);
            RefreshCard(card);
        }

        _eroded.Clear();
        _erodedThisTurn = 0;
    }

    // ───── 乱码刷新 ─────
    private void StartTicker()
    {
        if (_ticker != null && GodotObject.IsInstanceValid(_ticker)) return;

        var room = NCombatRoom.Instance;
        if (room == null) return;

        _ticker = new Timer
        {
            // 0.15s：标题/能量数字跳频略降，肉眼几乎无差别，开销比 0.1s 少 1/3
            WaitTime = 0.15f,
            OneShot = false,
            Autostart = true
        };
        _ticker.Timeout += RefreshErodedCards;
        room.AddChildSafely(_ticker);
    }

    private void StopTicker()
    {
        if (_ticker != null && GodotObject.IsInstanceValid(_ticker))
        {
            _ticker.Stop();
            _ticker.QueueFreeSafely();
        }

        _ticker = null;
    }

    private void RefreshErodedCards()
    {
        if (_eroded.Count == 0)
        {
            StopTicker();
            return;
        }

        // 可能已被消耗/移出战斗的牌顺手清掉
        _eroded.RemoveWhere(card => card == null);

        // ticker 只做轻量文本刷新（换乱码字），不调用 UpdateVisuals，
        // 否则每秒 10 次 × N 张卡会明显掉帧。
        RefreshTexts(_eroded);
    }

    /// <summary>
    ///     轻量刷新：平时只刷**手牌区**（牌堆界面里的卡不实时跳，省开销）；
    ///     有 overlay（牌堆/图鉴/查看）打开时才扩到全 UI 树，让打开时也能看到乱码跳动。
    ///     一次遍历建好"卡 → 节点"映射，复杂度 O(节点数) 而不是 O(卡数 × 树大小)。
    ///     匹配用 ModelId（牌堆界面的 NCard 持有的是反序列化出来的另一份实例，引用相等会漏）。
    /// </summary>
    private static void RefreshTexts(IReadOnlyCollection<CardModel> cards)
    {
        var wanted = new HashSet<ModelId>(cards.Select(c => c.Id));
        var nodes = new List<NCard>();

        foreach (var root in RefreshRoots())
        foreach (var node in EnumerateCards(root))
        {
            if (!GodotObject.IsInstanceValid(node) || node.Model is not { } model) continue;
            if (!wanted.Contains(model.Id)) continue;
            nodes.Add(node);
        }

        foreach (var node in nodes)
            ViolaErosionVisual.RefreshTextsOnly(node);
    }

    /// <summary>平时只有手牌区；牌堆/查看界面打开时扩到全 UI 树。</summary>
    private static IEnumerable<Node> RefreshRoots()
    {
        var room = NCombatRoom.Instance;

        if (room == null)
        {
            yield break;
        }

        if (IsAnyOverlayOpen())
        {
            yield return room;

            var ui = NRun.Instance?.GlobalUi;
            if (ui != null && GodotObject.IsInstanceValid(ui)) yield return ui;

            yield break;
        }

        // 只刷手牌区
        var hand = room.Ui?.Hand;
        if (hand != null && GodotObject.IsInstanceValid(hand)) yield return hand;
    }

    /// <summary>
    ///     有任意全屏界面打开时返回 true。
    ///     注意：牌堆/图鉴界面是 ICapstoneScreen，挂在 NCapstoneContainer 下（源码 NCapstoneContainer.cs:30-32），
    ///     **不进 NOverlayStack**，所以必须查 CapstoneContainer.InUse，只查 ScreenCount 永远为 false。
    /// </summary>
    private static bool IsAnyOverlayOpen()
    {
        var capstone = NCapstoneContainer.Instance;
        if (capstone != null && GodotObject.IsInstanceValid(capstone) && capstone.InUse) return true;

        var stack = NOverlayStack.Instance;
        return stack != null && GodotObject.IsInstanceValid(stack) && stack.ScreenCount > 0;
    }

    /// <summary>完整刷新：刚挂上/摘下侵蚀时用一次，重贴卡图与卡框并重排。</summary>
    private static int RefreshCard(CardModel card)
    {
        card.Owner?.PlayerCombatState?.RecalculateCardValues();

        var hit = 0;
        foreach (var root in Roots())
        {
            foreach (var node in EnumerateCards(root))
            {
                if (!GodotObject.IsInstanceValid(node) || !ReferenceEquals(node.Model, card)) continue;
                node.UpdateVisuals(PileType.Hand, CardPreviewMode.None);

                // UpdateVisuals 会把能量数字与卡框还原，刷新后再盖一次
                ViolaErosionVisual.ApplyEnergyGarbage(node);
                ViolaErosionVisual.ApplyFrameTint(node);
                ViolaErosionVisual.ApplyTitle(node);
                hit++;
            }
        }

        return hit;
    }

    private static IEnumerable<Node> Roots()
    {
        // 战斗房间（手牌/牌堆特效）、全局 UI（牌堆界面、悬浮预览）
        var room = (Node?)NCombatRoom.Instance;
        if (room != null && GodotObject.IsInstanceValid(room)) yield return room;

        var ui = NRun.Instance?.GlobalUi;
        if (ui != null && GodotObject.IsInstanceValid(ui)) yield return ui;
    }

    private static IEnumerable<NCard> EnumerateCards(Node parent)
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is NCard card)
                yield return card;

            foreach (var nested in EnumerateCards(child))
                yield return nested;
        }
    }
}
