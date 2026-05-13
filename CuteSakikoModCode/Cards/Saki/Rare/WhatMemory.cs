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

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class WhatMemory() : CuteSakikoModCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    // 基础选择数量 1，升级后变为 2
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new CardsVar(1); }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Sakiforget);
            yield return ModKeywordRegistry.CreateHoverTip(CutesakiKeywords.Memory);
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int maxSelect = DynamicVars.Cards.IntValue;   // 读取当前选择数量（1 或 2）

        var prompt = new LocString("cards", "CUTE_SAKIKO_MOD_CARD_WHAT_MEMORY.selectionScreenPrompt");
        prompt.Add("Cards", (decimal)maxSelect);       // 手动注入变量

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
            card.EnergyCost.SetThisCombat(0, true);
            card.AddModKeyword(CutesakiKeywords.Memory);
            card.AddModKeyword(CutesakiKeywords.Sakiforget);
        }
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
        DynamicVars.Cards.UpgradeValueBy(1);   // 1 → 2
    }
}