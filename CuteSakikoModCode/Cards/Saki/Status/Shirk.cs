using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Status;

[Pool(typeof(StatusCardPool))]
public class Shirk() : CustomCardModel(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Ethereal];
    public override int MaxUpgradeLevel => 0;

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;

        await Cmd.Wait(0.25f);

        var otherCards = PileType.Hand.GetPile(Owner).Cards
            .Where(c => c != this)
            .ToList();

        if (otherCards.Count > 0)
        {
            var randomCard = Owner.RunState.Rng.CombatCardSelection.NextItem(otherCards);
            await CardCmd.Discard(choiceContext, randomCard);
        }
    }
}