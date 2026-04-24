
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class SugarOverload : CuteAnonCard
    {
        [SavedProperty]
        private int _timesPlayedThisCombat;

        public SugarOverload() : base(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
        {
        }

        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                yield return CardKeyword.Ethereal;
            }
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
            EnergyCost.UpgradeBy(1); // 每次打出费用永久 +1
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Energy.UpgradeValueBy(1m);   // 2 → 3
            DynamicVars.Cards.UpgradeValueBy(1m);    // 1 → 2
        }
    }
}