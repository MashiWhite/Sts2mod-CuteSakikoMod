using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Cards;
using CuteSakikoMod.CuteSakikoModCode.Monsters.Weak;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class ShowOffPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // ★ 改为全局标记：本回合是否已经有玩家打出过第一张牌
    private bool _hasTriggeredThisTurn;

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 如果本回合已经触发过，直接忽略后续所有的牌
        if (_hasTriggeredThisTurn) return Task.CompletedTask;

        // 标记已触发
        _hasTriggeredThisTurn = true;

        if (Owner.Monster is AnonWang anonWang)
        {
            anonWang.ReactToCard(cardPlay.Card.Type);
        }

        return Task.CompletedTask;
    }

    public override Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        // 玩家回合结束时清空标记，下回合重新计算
        if (side == CombatSide.Player)
            _hasTriggeredThisTurn = false;
        return Task.CompletedTask;
    }
}