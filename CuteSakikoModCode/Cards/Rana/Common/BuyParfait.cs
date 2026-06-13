using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Common;


public class BuyParfait() : CuteRanaCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Parfait.GetModCardKeyword());
        }
    }
    
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(15)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int cost = DynamicVars.Gold.IntValue;
        await PlayerCmd.LoseGold(cost, Owner);

        var parfait = Owner.Relics.OfType<MatchaParfait>().FirstOrDefault();
        if (parfait != null)
            MatchaParfait.AddCharges(parfait, 1);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}