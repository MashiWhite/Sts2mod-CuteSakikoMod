using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;
using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Rare;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
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
        get { yield return new DynamicVar("GoldPrice", GoldPrice); }
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

        // 创建三张 Token 的副本（不直接加入手牌）
        var tokens = new List<CardModel>
        {
            CombatState.CreateCard<BuyGold>(player),
            CombatState.CreateCard<SellGold>(player),
            CombatState.CreateCard<GoldBrick>(player)
        };

        // 如果 HbkBank 已升级，Token 也跟着升级
        if (_parentCard != null && _parentCard.IsUpgraded)
            foreach (var token in tokens)
                if (token.IsUpgradable)
                    CardCmd.Upgrade(token);

        // 弹出选择界面，必须选一张
        var prompt = new LocString("powers", "HBK_BANK_POWER.chooseToken"); // 本地化提示
        var prefs = new CardSelectorPrefs(prompt, 1, 1); // 最少选1，最多选1

        var selectedCards = await CardSelectCmd.FromSimpleGrid(choiceContext, tokens, player, prefs);
        var chosen = selectedCards.FirstOrDefault();
        if (chosen != null)
            await CardPileCmd.AddGeneratedCardToCombat(chosen, PileType.Hand, player);
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