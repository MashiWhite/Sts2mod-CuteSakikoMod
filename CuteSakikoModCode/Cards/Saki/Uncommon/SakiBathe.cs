using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;
using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class SakiBathe() : CuteSakikoModCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private static bool _isTransforming;

    public override IEnumerable<CardKeyword> CanonicalKeywords => new CardKeyword[] { };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new CardsVar(1);
            yield return new EnergyVar(1);
        }
    }

    protected override PileType GetResultPileTypeForCardPlay()
    {
        var pressure = Owner.Creature.GetPower<PressurePower>();
        var required = IsUpgraded ? 10 : 15;
        var enough = pressure != null && pressure.Amount >= required;
        return enough ? PileType.Hand : PileType.Discard;
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        await PlayerCmd.GainEnergy(DynamicVars.Energy.BaseValue, Owner);

        var pressure = Owner.Creature.GetPower<PressurePower>();
        var required = IsUpgraded ? 10 : 15;
        if (pressure != null && pressure.Amount >= required)
            await PowerCmd.ModifyAmount(choiceContext, pressure, -required, Owner.Creature, this);

        await Cmd.CustomScaledWait(0.1f, 0.15f);
        await TryCombine();
    }

    private async Task TryCombine()
    {
        var lockObj = typeof(SakiBathe);
        var lockTaken = false;
        try
        {
            Monitor.Enter(lockObj, ref lockTaken);
            if (_isTransforming) return;
            _isTransforming = true;
        }
        finally
        {
            if (lockTaken) Monitor.Exit(lockObj);
        }

        try
        {
            if (CombatState == null || Owner == null) return;

            var hand = PileType.Hand.GetPile(Owner);
            if (hand == null) return;

            var anonCard = hand.Cards.OfType<AnonBathe>().FirstOrDefault();
            if (anonCard == null) return;

            var shouldUpgrade = IsUpgraded || anonCard.IsUpgraded;

            if (anonCard.Pile != null && anonCard.Pile.IsCombatPile && anonCard.Pile.Type != PileType.Play &&
                anonCard.Pile.Type != PileType.Exhaust) await CardPileCmd.RemoveFromCombat(anonCard);

            if (Pile != null && Pile.IsCombatPile && Pile.Type != PileType.Play &&
                Pile.Type != PileType.Exhaust) await CardPileCmd.RemoveFromCombat(this);

            var combined = CombatState.CreateCard<AnonSakiBathe>(Owner);
            if (shouldUpgrade)
            {
                combined.UpgradeInternal();
                combined.FinalizeUpgradeInternal();
            }

            await CardPileCmd.AddGeneratedCardToCombat(combined, PileType.Hand, Owner);
        }
        finally
        {
            _isTransforming = false;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1m);
        DynamicVars.Energy.UpgradeValueBy(1m);
    }
}