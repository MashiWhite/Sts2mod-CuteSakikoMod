using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Status;

[Pool(typeof(StatusCardPool))]
public class MyTreat() : CustomCardModel(1, CardType.Status, CardRarity.Status, TargetType.Self)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    // 没有 Unplayable，可以主动打出
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Ethereal];

    // 升级可降低费用（从1变为0）
    public override int MaxUpgradeLevel => 1;

    protected override IEnumerable<DynamicVar> CanonicalVars => [];

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
        await PlayerCmd.LoseGold(5, Owner);
    }

    protected override void OnUpgrade()
    {
        // 升级：费用从1变为0
        EnergyCost.UpgradeBy(-1);
    }
}