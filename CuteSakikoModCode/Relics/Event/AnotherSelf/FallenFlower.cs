using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event.AnotherSelf;

/// <summary>
///     落下的花：每回合第一次能量变为 0 时，获得 1 点能量。
///     主触发点：卡牌支付费用后能量归零（AfterEnergySpent，含 X 费）。
///     兜底：卡牌结算完仍为 0（例如被卡牌效果扣光能量），AfterCardPlayed 再判一次。
///     每回合限一次；回合开始能量重置时解锁（与 ArtOfWar 同款挂点）。
/// </summary>
public class FallenFlower : CuteSakikoEventRelic
{
    private bool _usedThisTurn;

    public override RelicRarity Rarity => RelicRarity.Ancient;

    public override Task AfterEnergyReset(Player player)
    {
        if (player != Owner) return Task.CompletedTask;
        _usedThisTurn = false;
        return Task.CompletedTask;
    }

    public override Task AfterEnergySpent(CardModel card, int amount)
    {
        if (card.Owner != Owner) return Task.CompletedTask;
        return TryTrigger();
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card?.Owner != Owner) return Task.CompletedTask;
        return TryTrigger();
    }

    private async Task TryTrigger()
    {
        if (_usedThisTurn) return;

        var player = Owner;
        var state = player?.PlayerCombatState;
        if (state == null || state.Energy != 0) return;

        _usedThisTurn = true;
        Flash();
        await PlayerCmd.GainEnergy(1m, player!);
    }
}
