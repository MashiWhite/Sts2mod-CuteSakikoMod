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
    public override PowerStackType StackType => PowerStackType.Counter;

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        if (Amount <= 0) return;

        var forgetPile = ForgetCardPile.Get(Owner.Player);
        if (forgetPile == null || forgetPile.Cards.Count == 0) return;

        if (Amount >= forgetPile.Cards.Count)
        {
            // ★ 只按 Id.Entry 排序
            var allCards = forgetPile.Cards
                .OrderBy(c => c.Id.Entry, StringComparer.Ordinal)
                .ToList();

            foreach (var card in allCards)
            {
                await CardCmd.AutoPlay(choiceContext, card, null);

                if (card.Pile != null && card.Pile != forgetPile)
                    await CardPileCmd.Add(card, forgetPile);
            }

            forgetPile.InvokeContentsChanged();
        }
        else
        {
            var toSelect = Math.Min(Amount, forgetPile.Cards.Count);
            var customPrompt = new LocString("powers", "CUTE_SAKIKO_MOD_TO_FORGET");
            var prefs = new CardSelectorPrefs(customPrompt, toSelect);

            // ★ 只按 Id.Entry 排序
            var candidates = forgetPile.Cards
                .OrderBy(c => c.Id.Entry, StringComparer.Ordinal)
                .ToList();

            var selected = await CardSelectCmd.FromSimpleGrid(
                choiceContext,
                candidates,
                Owner.Player,
                prefs
            );

            // ★ 只按 Id.Entry 排序
            var selectedList = selected
                .OrderBy(c => c.Id.Entry, StringComparer.Ordinal)
                .ToList();

            foreach (var card in selectedList)
            {
                await CardCmd.AutoPlay(choiceContext, card, null);

                if (card.Pile != null && card.Pile != forgetPile)
                    await CardPileCmd.Add(card, forgetPile);
            }

            forgetPile.InvokeContentsChanged();
        }
    }
}