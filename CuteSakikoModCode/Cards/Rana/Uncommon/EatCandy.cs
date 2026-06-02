using CuteSakikoMod.CuteSakikoModCode.Cards.Anon;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Uncommon;

public class EatCandy : CuteRanaCard
{
    public EatCandy() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self) { }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new EnergyVar(2);
            yield return new CardsVar(2);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int energy = DynamicVars.Energy.IntValue;
        int draw = DynamicVars.Cards.IntValue;

        if (energy > 0)
            await PlayerCmd.GainEnergy(energy, Owner);
        if (draw > 0)
            await CardPileCmd.Draw(choiceContext, draw, Owner);

        // 递减（不低于0）
        if (energy > 0) DynamicVars.Energy.UpgradeValueBy(-1);
        if (draw > 0) DynamicVars.Cards.UpgradeValueBy(-1);
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Ethereal);
        DynamicVars.Cards.UpgradeValueBy(1);
    }
}