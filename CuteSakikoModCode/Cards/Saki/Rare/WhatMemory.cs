
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;


namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;


public class WhatMemory() : CuteSakikoModCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Memory);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var allCards = new List<CardModel>();
        foreach (var pileType in new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust })
        {
            var pile = pileType.GetPile(Owner);
            if (pile != null) allCards.AddRange(pile.Cards);
        }
        allCards = allCards.Distinct().ToList();
        foreach (var card in allCards)
        {
            card.EnergyCost.SetThisCombat(0, true);
            // 关键：用扩展方法 AddModKeyword，不要用 CardCmd.ApplyKeyword
            card.AddModKeyword(CutesakiKeywords.Memory);
        }
        await Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
        EnergyCost.UpgradeBy(-1);
    }
}