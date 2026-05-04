using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class Exhaustion() : CuteAnonCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new CardsVar(1);
            yield return new EnergyVar(1);
        }
    }

    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            if (Owner == null) return false;
            var hand = PileType.Hand.GetPile(Owner);
            return hand?.Cards.Count == 1;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var drawCount = DynamicVars["Cards"].IntValue;
        var energyGain = DynamicVars["Energy"].IntValue;

        await CardPileCmd.Draw(choiceContext, drawCount, Owner);
        await PlayerCmd.GainEnergy(energyGain, Owner);
    }

    // ---------- 手牌变化钩子 ----------
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayed(choiceContext, cardPlay);
        if (cardPlay.Card.Owner != Owner) return;
        UpdateCostBasedOnHand();
    }

    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        await base.AfterCardDrawn(choiceContext, card, fromHandDraw);
        // 任何牌被抽出后都更新费用（因为手牌数可能受影响）
        UpdateCostBasedOnHand();
    }


    // ---------- 费用更新逻辑 ----------
    private void UpdateCostBasedOnHand()
    {
        if (Owner == null) return;
        var hand = PileType.Hand.GetPile(Owner);
        var onlyThisCard = hand?.Cards.Count == 1;
        EnergyCost.SetThisTurn(onlyThisCard ? 0 : 1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Cards"].UpgradeValueBy(1m);
    }
}