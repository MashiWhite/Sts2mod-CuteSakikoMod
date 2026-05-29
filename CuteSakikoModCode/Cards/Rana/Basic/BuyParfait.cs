using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Relics.Rana.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Basic;

[RegisterCharacterStarterCard(typeof(CuteRana), 1, Order = 2)]
public class BuyParfait() : CuteRanaCard(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new GoldVar(10)
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
        DynamicVars.Gold.UpgradeValueBy(-5m);  // 10 -> 5
    }
}