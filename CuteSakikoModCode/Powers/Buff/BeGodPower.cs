using System.Linq;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using CuteSakikoMod.CuteSakikoModCode.CardPiles;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class BeGodPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;
        if (Amount <= 0) return;

        var forgetPile = ForgetCardPile.Get(Owner.Player);
        if (forgetPile == null || forgetPile.Cards.Count == 0) return;

        var toPlay = System.Math.Min(Amount, forgetPile.Cards.Count);
        var rng = Owner.CombatState.RunState.Rng.CombatCardSelection;

        // 复制并洗牌
        var cardsToPlay = forgetPile.Cards.ToList();
        for (var i = cardsToPlay.Count - 1; i > 0; i--)
        {
            var j = rng.NextInt(i + 1);
            (cardsToPlay[i], cardsToPlay[j]) = (cardsToPlay[j], cardsToPlay[i]);
        }

        var selected = cardsToPlay.Take(toPlay).ToList();

        foreach (var card in selected)
        {
            // 从遗忘堆正常移出（会触发事件，不残留节点）
            if (card.Pile == forgetPile)
                forgetPile.RemoveInternal(card);
            else
                card.RemoveFromCurrentPile();

            // 飞入手牌（有动画）
            await CardPileCmd.Add(card, PileType.Hand);

            // 自动打出
            await CardCmd.AutoPlay(choiceContext, card, null);

            // 打出后再飞回遗忘堆（有动画）
            if (card.Pile != null)
                await CardPileCmd.Add(card, forgetPile);
        }

        // 最后统一更新一次按钮数字
        forgetPile.InvokeContentsChanged();
    }
}