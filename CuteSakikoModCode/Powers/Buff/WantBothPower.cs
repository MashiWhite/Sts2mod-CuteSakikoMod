using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public class WantBothPower : CuteSakikoModPower
{
    private bool _subscribed;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single; // 不可叠层

    // 能力被施加时订阅事件
    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        if (!_subscribed)
        {
            MatchaParfait.OnChargesRemoved += OnParfaitConsumed;
            _subscribed = true;
        }
    }

    // 能力被移除时取消订阅
    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_subscribed)
        {
            MatchaParfait.OnChargesRemoved -= OnParfaitConsumed;
            _subscribed = false;
        }
        await base.AfterRemoved(oldOwner);
    }

    private async void OnParfaitConsumed(Player player, int amount, PlayerChoiceContext? choiceContext)
    {
        // 只对拥有本能力的玩家生效
        if (player != Owner.Player) return;
        // 确保还在战斗中且能力未移除（安全检查）
        if (Amount <= 0) return;

        // 需要有效的 PlayerChoiceContext 来执行抽牌和加能量
        if (choiceContext == null) return;

        await PlayerCmd.GainEnergy(1, player);
        await CardPileCmd.Draw(choiceContext, 1, player);
    }
}