using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Status;

public class Fraud() : ModStatusCard(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
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