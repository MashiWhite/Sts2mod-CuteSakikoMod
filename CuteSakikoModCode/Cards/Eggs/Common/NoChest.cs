using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Other;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Eggs.Common;

[Pool(typeof(CuteSakikoEggCardPool))]
public class NoChest : CustomCardModel
{
    private int _totalReduction;

    public NoChest() : base(12, CardType.Power, CardRarity.Common, TargetType.Self)
    {
    }

    public override CardMultiplayerConstraint MultiplayerConstraint => CardMultiplayerConstraint.MultiplayerOnly;

    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    [SavedProperty]
    public int TotalReduction
    {
        get => _totalReduction;
        set
        {
            _totalReduction = value;
            if (Owner?.Creature?.CombatState != null)
                UpdateAllInstancesCost();
        }
    }

    // 固有（Innate）和保留（Retain）
    public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Innate };

    protected override IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PlatingPower>();
            yield return HoverTipFactory.FromCard<LookNoChest>(IsUpgraded);
            if (IsUpgraded)
                yield return HoverTipFactory.FromPower<BarricadePower>();
        }
    }

    // 抽到此牌时触发
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;
        await DistributeLookCards();
    }

    private async Task DistributeLookCards()
    {
        var owner = Owner;
        if (owner == null) return;
        var combatState = owner.Creature.CombatState;
        if (combatState == null) return;

        // 获取所有其他队友（排除自己）
        var allies = combatState.Players.Where(p => p != owner).ToList();
        foreach (var ally in allies)
        {
            var lookCard = combatState.CreateCard<LookNoChest>(ally);
            if (IsUpgraded)
                CardCmd.Upgrade(lookCard);
            await CardPileCmd.AddGeneratedCardToCombat(lookCard, PileType.Hand, true);
        }
    }

    // 打出时获得覆甲和升级壁垒
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        UpdateCost(); // 更新当前实例费用（实际上费用在抽到时已更新，但保险）

        await PowerCmd.Apply<PlatingPower>(Owner.Creature, 12, Owner.Creature, this);
        if (IsUpgraded) await PowerCmd.Apply<BarricadePower>(Owner.Creature, 1, Owner.Creature, this);
    }

    private void UpdateCost()
    {
        var newCost = 12 - TotalReduction;
        if (newCost < 0) newCost = 0;
        EnergyCost.SetThisCombat(newCost);
    }

    private void UpdateAllInstancesCost()
    {
        var combatState = Owner?.Creature?.CombatState;
        if (combatState == null) return;
        var allCards = Owner?.PlayerCombatState?.AllCards;
        if (allCards == null) return;
        foreach (var card in allCards.OfType<NoChest>()) card.UpdateCost();
    }

    // 由 LookNoChest 调用，减少费用
    public void ApplyReduction(int amount)
    {
        if (amount <= 0) return;
        TotalReduction += amount;
    }
}