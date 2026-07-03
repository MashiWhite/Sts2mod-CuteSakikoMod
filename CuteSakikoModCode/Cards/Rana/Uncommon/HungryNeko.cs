
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
        get { yield return new CardsVar(1); } // 用于描述需要消耗的数量
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
        int count = DynamicVars.Cards.IntValue;
        var hand = PileType.Hand.GetPile(Owner);
        int handCount = hand?.Cards.Count ?? 0;
        int actualSelect = Math.Min(count, handCount);

        if (actualSelect > 0)
        {
            var prefs = new CardSelectorPrefs(
                new LocString("cards", "CUTE_SAKIKO_MOD_CARD_HUNGRY_NEKO.selectionScreenPrompt"),
                actualSelect,
                actualSelect
            );
            var selected = await CardSelectCmd.FromHand(choiceContext, Owner, prefs, null, this);
            foreach (var card in selected)
            {
                await CardCmd.Exhaust(choiceContext, card);
            }
        }

        // 视为食用了 count 杯抹茶芭菲（无论实际消耗了几张牌）
        MatchaParfait.SimulateParfaitEaten(Owner, count, choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1); // 1 -> 2
    }
}