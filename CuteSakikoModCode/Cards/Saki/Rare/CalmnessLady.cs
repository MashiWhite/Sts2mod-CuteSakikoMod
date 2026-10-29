using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class CalmnessLady() : CuteSakikoModCard(1, CardType.Power, CardRarity.Rare, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DynamicVar("Layers", 1)
    ];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<CalmnessLadyPower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var read = DynamicVars["Layers"].IntValue;
        await PowerCmd.Apply<CalmnessLadyPower>(choiceContext, Owner.Creature, read, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Layers"].UpgradeValueBy(1m);
    }
}