using CuteSakikoMod.CuteSakikoModCode.Cards.Mod;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Status;

public class MyTreat() : ModStatusCard(0, CardType.Status, CardRarity.Status, TargetType.Self)
{
#if STS2_AT_LEAST_0_113_0
    // 0.113 Blitz（闪局）禁用：闪局没有金币经济（商店改成免费 6 选 3），金币类卡牌无意义
    public override bool IsAllowedInBlitz => false;
#endif
    // 没有 Unplayable，可以主动打出
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    // 升级可降低费用（从1变为0）
    public override int MaxUpgradeLevel => 1;

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(5)
    ];

    // 打出时：抽一张牌
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, 1, Owner);
    }

    // 抽到时：减少5金币
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;
        await Cmd.Wait(0.25f);
        await PlayerCmd.LoseGold(DynamicVars.Gold.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Gold.UpgradeValueBy(-2);
    }
}