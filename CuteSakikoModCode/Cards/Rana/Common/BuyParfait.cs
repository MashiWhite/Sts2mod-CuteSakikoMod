using CuteSakikoMod.CuteSakikoModCode.Others;
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
#if STS2_AT_LEAST_0_113_0
    // 0.113 Blitz（闪局）禁用：闪局没有金币经济（商店改成免费 6 选 3），金币类卡牌无意义
    public override bool IsAllowedInBlitz => false;
#endif
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.Parfait.GetModCardKeyword(), CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromKeyword(CutesakiKeywords.Parfait.GetModCardKeyword()); }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(10)
    ];

    // 金币不足时无法打出
    protected override bool IsPlayable => Owner.Gold >= DynamicVars.Gold.IntValue;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var cost = DynamicVars.Gold.IntValue;
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