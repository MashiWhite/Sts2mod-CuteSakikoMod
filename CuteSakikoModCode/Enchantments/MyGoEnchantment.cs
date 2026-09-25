using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Enchantments;

[RegisterEnchantment]
public sealed class MyGoEnchantment : ModEnchantmentTemplate
{
    public override bool ShowAmount => false;
    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new(
        "CuteSakikoMod/images/enchantments/mygo.png"
    );

    private static readonly Lazy<PileType?> _forgetPileType = new(() =>
    {
        try { return ForgetCardPile.GetPileType(); }
        catch { return null; }
    });

    private Player? _pendingDeckOwner;

    public override bool CanEnchant(CardModel card) => true;

    public override CardLocation ModifyCardPlayResultLocation(
        CardModel card,
        bool isAutoPlay,
        ResourceInfo resources,
        CardLocation cardLocation)
    {
        if (card != Card) return cardLocation;
        if (card.IsDupe || card.Type == CardType.Power) return cardLocation;

        var combatState = card.CombatState;
        if (combatState == null) return cardLocation;

        var players = combatState.Players.ToList();
        if (players.Count == 0) return cardLocation;

        var piles = new List<PileType>
        {
            PileType.Draw,
            PileType.Discard,
            PileType.Exhaust,
            PileType.Hand,
            PileType.None,
        };
        if (_forgetPileType.Value is { } forget)
            piles.Add(forget);

        var rng = card.Owner.RunState.Rng.CombatCardGeneration;
        var targetPlayer = players[rng.NextInt(players.Count)];

        // 追加“进牌组”分支
        int roll = rng.NextInt(piles.Count + 1);
        if (roll == piles.Count)
        {
            _pendingDeckOwner = targetPlayer;
            return new CardLocation(card.Owner, PileType.None, CardPilePosition.Bottom);
        }

        return new CardLocation(targetPlayer, piles[roll], CardPilePosition.Bottom);
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        await base.OnPlay(choiceContext, cardPlay);

        var deckOwner = _pendingDeckOwner;
        if (deckOwner == null) return;
        _pendingDeckOwner = null;

        var canonical = ModelDb.GetById<CardModel>(Card.Id);
        var newCard = deckOwner.RunState.CreateCard(canonical, deckOwner);

        int upgradeCount = Math.Min(Card.CurrentUpgradeLevel, newCard.MaxUpgradeLevel);
        for (int i = 0; i < upgradeCount; i++)
            CardCmd.Upgrade(newCard);

        if (Card.Enchantment != null)
        {
            var ench = (EnchantmentModel)Card.Enchantment.MutableClone();
            if (ench.CanEnchant(newCard))
                CardCmd.Enchant(ench, newCard, ench.Amount);
        }

        await CardPileCmd.Add(newCard, PileType.Deck);
        
        PileType.Deck.GetPile(deckOwner).InvokeCardAddFinished();
    }
}