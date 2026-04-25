
using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;
using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Uncommon;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;


namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class HbkBankPower : CuteSakikoModPower
{
    private HbkBank _parentCard;
    
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public int GoldPrice { get; private set; } = 10;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new IntVar("GoldPrice", GoldPrice); }
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        _parentCard = cardSource as HbkBank;
        GoldPrice = 10;
        UpdateDynamicVar();
    }

    private void UpdateDynamicVar()
    {
        if (DynamicVars.TryGetValue("GoldPrice", out var var))
            var.BaseValue = GoldPrice;
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        var buy = CombatState.CreateCard<BuyGold>(player);
        var sell = CombatState.CreateCard<SellGold>(player);
        if (_parentCard != null && _parentCard.IsUpgraded)
        {
            if (buy.IsUpgradable) CardCmd.Upgrade(buy);
            if (sell.IsUpgradable) CardCmd.Upgrade(sell);
        }

        await CardPileCmd.AddGeneratedCardToCombat(buy, PileType.Hand, player);
        await CardPileCmd.AddGeneratedCardToCombat(sell, PileType.Hand, player);
    }

    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;
        if (CombatState == null) return;

        var range = _parentCard != null && _parentCard.IsUpgraded ? 20 : 10;
        var delta = CombatState.RunState.Rng.CombatCardSelection.NextInt(-range, range + 1);
        GoldPrice += delta;
        if (GoldPrice < 1) GoldPrice = 1;
        UpdateDynamicVar();
    }
}