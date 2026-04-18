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
public class Fraud() : CustomCardModel(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
    public override string PortraitPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

    // 不能被打出，虚无
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Unplayable, CardKeyword.Ethereal];


    // 无法升级
    public override int MaxUpgradeLevel => 0;

    // 动态变量：能量损失值（固定1）
    protected override IEnumerable<DynamicVar> CanonicalVars => [new EnergyVar(1)];

    // 抽到时失去1点能量
    public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
    {
        if (card != this) return;
        await Cmd.Wait(0.25f);
        await PlayerCmd.LoseEnergy(DynamicVars.Energy.IntValue, Owner);
    }
}