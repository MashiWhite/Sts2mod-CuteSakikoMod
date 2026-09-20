using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class Parry() : CuteSakikoModCard(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<SakiParryPower>(6m)
    ];
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromCard<KnightSword>();
            yield return HoverTipFactory.FromPower<SakiParryPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var layers = DynamicVars["SakiParryPower"].BaseValue;
        await PowerCmd.Apply<SakiParryPower>(choiceContext, Owner.Creature, layers, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["SakiParryPower"].UpgradeValueBy(3);
    }
}