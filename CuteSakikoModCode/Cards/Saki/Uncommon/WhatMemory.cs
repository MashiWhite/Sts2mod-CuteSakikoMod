using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class WhatMemory() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];
    
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new CardsVar(2); }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Memory.GetModCardKeyword());
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Sakiforget.GetModCardKeyword());
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var maxSelect = DynamicVars.Cards.IntValue; // 读取当前选择数量（1 或 2）

        var prompt = new LocString("cards", "CUTE_SAKIKO_MOD_CARD_WHAT_MEMORY.selectionScreenPrompt");
        prompt.Add("Cards", maxSelect); // 手动注入变量

        var prefs = new CardSelectorPrefs(prompt, 0, maxSelect)
        {
            RequireManualConfirmation = true
        };

        var selectedCards = await CardSelectCmd.FromHand(
            choiceContext,
            Owner,
            prefs,
            c => c != this,
            this
        );

        foreach (var card in selectedCards)
        {
            card.EnergyCost.SetThisCombat(1, true);
            card.AddKeyword(CutesakiKeywords.Memory.GetModCardKeyword());
            card.AddKeyword(CutesakiKeywords.Sakiforget.GetModCardKeyword());
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}