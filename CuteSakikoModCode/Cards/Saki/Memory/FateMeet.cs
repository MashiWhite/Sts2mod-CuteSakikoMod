using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Scaffolding.Content;
using CuteSakikoMod.CuteSakikoModCode.Others;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Memory;

public class FateMeet() : SakiMemoryCard(0, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    // 覆盖 AssetProfile，指定先古样式
    public override CardAssetProfile AssetProfile => this.CardAssetProfile(CardVisualStyle.Ancient);

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new CardsVar(2)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 抽牌
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}