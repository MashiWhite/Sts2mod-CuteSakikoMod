using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class HungryNeko : CuteRanaCard
{
    public HungryNeko() : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new CardsVar(1); }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Parfait.GetModCardKeyword());
            yield return HoverTipFactory.FromKeyword(CardKeyword.Exhaust);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = DynamicVars.Cards.IntValue;          // 最大可选数量：升级前1，升级后2
        var hand = PileType.Hand.GetPile(Owner);
        int handCount = hand?.Cards.Count ?? 0;
        int maxSelect = Math.Min(count, handCount);

        // 最少可选 0 张，允许跳过
        var prefs = new CardSelectorPrefs(
            new LocString("cards", "CUTE_SAKIKO_MOD_CARD_HUNGRY_NEKO.selectionScreenPrompt"),
            0, maxSelect);

        var selected = await CardSelectCmd.FromHand(choiceContext, Owner, prefs, null, this);
        var selectedCards = selected.ToList();
        foreach (var card in selectedCards)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        // 食用杯数 = 实际消耗的手牌数量（选择了几张就吃几杯）
        int eaten = selectedCards.Count;
        if (eaten > 0)
            MatchaParfait.SimulateParfaitEaten(Owner, eaten, choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1); // 1 → 2
    }
}