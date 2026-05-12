using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;

public class PressureOption() : ModTokenCard(0, CardType.Skill, CardRarity.Token, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new PowerVar<PressurePower>(5m),
        new PowerVar<StrengthPower>(3m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PressurePower>(choiceContext, Owner.Creature,
            DynamicVars["PressurePower"].IntValue, Owner.Creature, this);
        await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature,
            DynamicVars["StrengthPower"].IntValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        if (DynamicVars.TryGetValue("PressurePower", out var pv))
            pv.UpgradeValueBy(3);
        if (DynamicVars.TryGetValue("StrengthPower", out var sv))
            sv.UpgradeValueBy(2);
    }
}