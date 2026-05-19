using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class SugarOverload() : CuteAnonCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    [SavedProperty] private int _timesPlayedThisCombat;

    public override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get { yield return CardKeyword.Ethereal; }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new EnergyVar(2);
            yield return new CardsVar(1);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        await PlayerCmd.GainEnergy(DynamicVars.Energy.IntValue, Owner);
        await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);

        _timesPlayedThisCombat++;
        // 使用 AddThisCombat 增加费用，避免设置 WasJustUpgraded 标记
        EnergyCost.AddThisCombat(1);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Energy.UpgradeValueBy(1m); // 2 → 3
        DynamicVars.Cards.UpgradeValueBy(1m); // 1 → 2
    }
}