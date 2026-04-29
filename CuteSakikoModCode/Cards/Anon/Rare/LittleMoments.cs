using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Token;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class LittleMoments() : CuteAnonCard(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    private static bool _isTransforming;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromCard<Lifetime>(IsUpgraded); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new CardsVar(2); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        if (CombatState != null && Owner != null)
        {
            var copy = CombatState.CreateCard<LittleMoments>(Owner);
            if (IsUpgraded)
            {
                copy.UpgradeInternal();
                copy.FinalizeUpgradeInternal();
            }

            await CardPileCmd.Add(copy, PileType.Draw, CardPilePosition.Random);
        }

        await Cmd.CustomScaledWait(0.1f, 0.15f);

        await TryTransform();
    }

    private async Task TryTransform()
    {
        if (_isTransforming) return;
        if (CombatState == null || Owner == null) return;

        // 仅检查手牌中的数量
        var hand = PileType.Hand.GetPile(Owner);
        if (hand == null) return;
        var handCount = hand.Cards.OfType<LittleMoments>().Count();
        if (handCount < 5) return;

        _isTransforming = true;

        // 但移除时，清理所有牌堆中的残留
        var allCards = Owner.PlayerCombatState?.AllCards;
        if (allCards != null)
        {
            var validCards = allCards.OfType<LittleMoments>()
                .Where(c => c.Pile != null
                            && c.Pile.IsCombatPile
                            && c.Pile.Type != PileType.Play
                            && c.Pile.Type != PileType.Exhaust
                            && CombatState.ContainsCard(c))
                .ToList();

            foreach (var card in validCards)
                await CardPileCmd.RemoveFromCombat(card);
        }

        var lifetime = CombatState.CreateCard<Lifetime>(Owner);
        if (IsUpgraded)
        {
            lifetime.UpgradeInternal();
            lifetime.FinalizeUpgradeInternal();
        }

        await CardPileCmd.AddGeneratedCardToCombat(lifetime, PileType.Hand, Owner);

        _isTransforming = false;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}