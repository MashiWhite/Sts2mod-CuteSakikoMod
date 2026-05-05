using CuteSakikoMod.CuteSakikoModCode.Enchantments;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class WhenPerformPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private bool _triggeredThisTurn;
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            var tips = new List<IHoverTip>(HoverTipFactory.FromEnchantment<PlayEnchantment>());
            return tips;
        }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner?.Player) return;
        _triggeredThisTurn = false;
        await Task.CompletedTask;
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        // 已经附魔过本回合，或不是自己的牌，或 Owner 无效，直接跳过
        if (_triggeredThisTurn || card.Owner != Owner?.Player || Owner is null) return;

        // 如果这张牌已有附魔，则忽略，继续等待下一张抽牌
        if (card.Enchantment != null) return;

        // 找到第一张无附魔的牌，附魔并标记本回合已完成
        CardCmd.Enchant<PlayEnchantment>(card, 1);
        _triggeredThisTurn = true;
    }
}