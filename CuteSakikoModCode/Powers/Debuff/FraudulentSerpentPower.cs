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
using Timer = Godot.Timer;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;

/// <summary>
///     欺诈之蛇：你每回合抽到的前 Amount 张牌无法辨认。
///     被侵蚀的牌：卡图换成 viola、标题与描述变成持续跳动的乱码（见 Patches/ViolaErosionPatches）。
///     打出一次即恢复原状；回合结束时其余未打出的也恢复；power 被移除时兜底清理。
/// </summary>
public sealed class FraudulentSerpentPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    private readonly HashSet<CardModel> _erodedThisTurn = [];
    private Timer? _ticker;

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
        if (_erodedThisTurn.Count >= (int)Amount)
        {
            Entry.Logger.Info($"[Viola] draw skip quota: count={_erodedThisTurn.Count} amount={Amount}");
            return;
        }

        if (!_erodedThisTurn.Add(card)) return;

        await CardCmd.Afflict<ViolaErosion>(card, 1);
        Entry.Logger.Info($"[Viola] afflict card={card.Id.Entry} total={_erodedThisTurn.Count} amount={Amount} affliction={card.Affliction?.Id.Entry ?? "null"}");
        StartTicker();
        RefreshCard(card);
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (card == null || !_erodedThisTurn.Contains(card)) return Task.CompletedTask;

        _erodedThisTurn.Remove(card);
        CardCmd.ClearAffliction(card);
        RefreshCard(card);
        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner?.Side) return Task.CompletedTask;

        ClearErosion();
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner)
    {
        ClearErosion();
        StopTicker();
        return Task.CompletedTask;
    }

    public void ClearErosion()
    {
        foreach (var card in _erodedThisTurn)
        {
            if (card == null || card.Affliction is not ViolaErosion) continue;

            CardCmd.ClearAffliction(card);
            RefreshCard(card);
        }

        _erodedThisTurn.Clear();
    }

    // ───── 乱码刷新 ─────
    private void StartTicker()
    {
        if (_ticker != null && GodotObject.IsInstanceValid(_ticker)) return;

        var room = NCombatRoom.Instance;
        Entry.Logger.Info($"[Viola] StartTicker room={(room == null ? "null" : "ok")} eroded={_erodedThisTurn.Count}");
        if (room == null) return;

        _ticker = new Timer
        {
            WaitTime = 0.1f,
            OneShot = false,
            Autostart = true
        };
        _ticker.Timeout += RefreshErodedCards;
        room.AddChildSafely(_ticker);
        Entry.Logger.Info($"[Viola] ticker added={GodotObject.IsInstanceValid(_ticker)}");
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
        if (_erodedThisTurn.Count == 0)
        {
            StopTicker();
            return;
        }

        var hit = 0;
        foreach (var card in _erodedThisTurn.ToList())
        {
            if (card == null) continue;
            hit += RefreshCard(card);
        }

        if (hit == 0)
            Entry.Logger.Info($"[Viola] tick no node hit, eroded={_erodedThisTurn.Count}");
    }

    private static int RefreshCard(CardModel card)
    {
        card.Owner?.PlayerCombatState?.RecalculateCardValues();

        // 手牌节点挂在 NCombatRoom 下（不是 NRun.GlobalUi），必须从房间根开始找
        var root = (Node?)NCombatRoom.Instance ?? NRun.Instance?.GlobalUi;
        if (root == null) return 0;

        var hit = 0;
        foreach (var node in EnumerateCards(root))
        {
            if (!GodotObject.IsInstanceValid(node) || !ReferenceEquals(node.Model, card)) continue;
            node.UpdateVisuals(PileType.Hand, CardPreviewMode.None);

            // UpdateVisuals 会把能量数字与卡框还原，刷新后再盖一次
            ViolaErosionVisual.ApplyEnergyGarbage(node);
            ViolaErosionVisual.ApplyFrameTint(node);
            hit++;
        }

        return hit;
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
