using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Combat;
using StringExtensions = BaseLib.Extensions.StringExtensions;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class BeGodPower : CustomPowerModel
{
    public override string CustomPackedIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").PowerImagePath();

    public override string CustomBigIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").PowerImagePath();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠层

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;
        if (Amount <= 0) return;

        var exhaustPile = PileType.Exhaust.GetPile(Owner.Player);
        if (exhaustPile == null || exhaustPile.Cards.Count == 0) return;

        int toPlay = Math.Min(Amount, exhaustPile.Cards.Count);
        var rng = Owner.CombatState.RunState.Rng.CombatCardSelection;

        // 复制列表并洗牌（Fisher-Yates）
        var cardsToPlay = exhaustPile.Cards.ToList();
        for (int i = cardsToPlay.Count - 1; i > 0; i--)
        {
            int j = rng.NextInt(i + 1);
            (cardsToPlay[i], cardsToPlay[j]) = (cardsToPlay[j], cardsToPlay[i]);
        }
        var selected = cardsToPlay.Take(toPlay).ToList();

        foreach (var card in selected)
        {
            if (card.Pile?.Type != PileType.Hand)
            {
                card.RemoveFromCurrentPile();
                await CardPileCmd.Add(card, PileType.Hand);
            }
            card.ExhaustOnNextPlay = true;
            await CardCmd.AutoPlay(choiceContext, card, null);
            if (card.Pile != null && card.Pile.Type != PileType.Exhaust)
            {
                await CardPileCmd.RemoveFromCombat(card);
            }
        }
    }
}