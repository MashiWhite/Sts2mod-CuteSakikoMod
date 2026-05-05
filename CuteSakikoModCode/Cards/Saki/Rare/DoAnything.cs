using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public sealed class DoAnything() : CuteSakikoModCard(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    private decimal _nextPressure = 8m;
    
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new CardsVar(1);
            yield return new PressureGainVar(_nextPressure);
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var prompt = new LocString("CUTESAKIKOMOD-DO_ANYTHING.prompt", "选择一张牌放入手牌");
        var prefs = new CardSelectorPrefs(prompt, 1);
        var drawPileCards = PileType.Draw.GetPile(Owner).Cards
            .OrderBy(c => c.Rarity)
            .ThenBy(c => c.Id)
            .ToList();

        var selectedCard = (await CardSelectCmd.FromSimpleGrid(
            choiceContext,
            drawPileCards,
            Owner,
            prefs)).FirstOrDefault();

        if (selectedCard != null) await CardPileCmd.Add(selectedCard, PileType.Hand);

        var pressureToGain = _nextPressure;
        await PowerCmd.Apply<PressurePower>(choiceContext, Owner.Creature, pressureToGain, Owner.Creature, this);

        _nextPressure *= 2;
        if (DynamicVars.TryGetValue("PressureGain", out var var)) var.BaseValue = _nextPressure;
    }

    protected override void OnUpgrade()
    {
        // 升级：添加固有（Innate）关键词，不改变压力值
        AddKeyword(CardKeyword.Innate);
    }
}

public class PressureGainVar : DynamicVar
{
    public PressureGainVar(decimal baseValue) : base("PressureGain", baseValue)
    {
    }
}