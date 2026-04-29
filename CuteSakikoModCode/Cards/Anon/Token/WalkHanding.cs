using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Token;

public class WalkHanding() : ModTokenCard(0, CardType.Power, CardRarity.Token, TargetType.AnyAlly)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get { yield return new PowerVar<WalkHandingPower>(6m); }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<EscapismTendencyPower>();
            yield return HoverTipFactory.FromPower<WalkHandingPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var target = cardPlay.Target;
        if (target == null) return;

        var escapismPower = target.GetPower<EscapismTendencyPower>();
        if (escapismPower == null) return;

        await PowerCmd.Remove(escapismPower);

        var amount = DynamicVars["WalkHandingPower"].IntValue;
        await PowerCmd.Apply<WalkHandingPower>(choiceContext, target, amount, Owner.Creature, this);
        await PowerCmd.Apply<WalkHandingPower>(choiceContext, Owner.Creature, amount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["WalkHandingPower"].UpgradeValueBy(3m); // 6-9
    }
}