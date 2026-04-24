
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;


namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class BearMemoryPower : CuteSakikoModPower
{
    // 静态字典，用于跟踪每张卡牌本次打出还需要获得的压力次数
    private static readonly Dictionary<CardModel, int> _pendingPressure = new();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    // 修改打出次数：回忆牌额外打出 Amount 次
    public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
    {
        // 只影响拥有者打出的回忆牌
        if (card.Owner?.Creature != Owner) return playCount;
        if (!card.CanonicalKeywords.Contains(CutesakiKeywords.Memory)) return playCount;

        var extra = Amount;
        if (extra <= 0) return playCount;

        // 记录需要额外施加的压力次数（等于额外打出次数）
        lock (_pendingPressure)
        {
            if (_pendingPressure.TryGetValue(card, out var existing))
                _pendingPressure[card] = existing + extra;
            else
                _pendingPressure[card] = extra;
        }

        return playCount + extra;
    }

    // 在每次卡牌打出后，检查是否需要施加压力
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        if (card.Owner?.Creature != Owner) return;

        lock (_pendingPressure)
        {
            if (_pendingPressure.TryGetValue(card, out var remaining) && remaining > 0)
            {
                // 减少计数，本次是额外打出的一次
                _pendingPressure[card] = remaining - 1;
                // 立即施加压力
                TaskHelper.RunSafely(PowerCmd.Apply<PressurePower>(Owner, 1, Owner, card));
            }
        }
    }

    // 回合结束时清理所有未消耗的记录（防止内存泄漏）
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == Owner.Side)
            lock (_pendingPressure)
            {
                _pendingPressure.Clear();
            }
    }
}