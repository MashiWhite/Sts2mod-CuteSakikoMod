using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class BeGodPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠层

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        if (Amount <= 0) return;

        var forgetPile = ForgetCardPile.Get(Owner.Player);
        if (forgetPile == null || forgetPile.Cards.Count == 0) return;

        var toSelect = Math.Min(Amount, forgetPile.Cards.Count);
        if (toSelect <= 0) return;

        // 自定义提示 LocString
        var customPrompt = new LocString("powers", "CUTE_SAKIKO_MOD_TO_FORGET");
        var prefs = new CardSelectorPrefs(customPrompt, toSelect);

        var selected = await CardSelectCmd.FromCombatPile(
            choiceContext,
            forgetPile,
            Owner.Player,
            prefs,
            _ => true
        );

        var selectedList = selected.ToList();
        foreach (var card in selectedList)
        {
            if (card.Pile == forgetPile)
                forgetPile.RemoveInternal(card);
            else
                card.RemoveFromCurrentPile();

            await CardPileCmd.Add(card, PileType.Hand);
            await CardCmd.AutoPlay(choiceContext, card, null);

            if (card.Pile != null)
                await CardPileCmd.Add(card, forgetPile);
        }

        forgetPile.InvokeContentsChanged();
    }
}