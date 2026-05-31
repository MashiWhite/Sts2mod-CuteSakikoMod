using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Token.Neko
{
    public class ParkingNekoNine : NekoCard
    {
        public ParkingNekoNine() : base(0, CardType.Skill, CardRarity.Token, TargetType.Self) { }

        protected override IEnumerable<DynamicVar> CanonicalVars => new[]
        {
            new EnergyVar(1)
        };

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            int energy = DynamicVars.Energy.IntValue;
            await PlayerCmd.GainEnergy(energy, Owner);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Energy.UpgradeValueBy(1);
        }
    }
}