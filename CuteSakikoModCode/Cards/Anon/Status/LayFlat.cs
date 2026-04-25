
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status
{
    [Pool(typeof(StatusCardPool))]
    public class LayFlat() : CustomCardModel(0, CardType.Status, CardRarity.Status, TargetType.Self)
    {
        public override string PortraitPath =>
            (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                yield return CardKeyword.Retain;
                yield return CardKeyword.Exhaust;
            }
        }

        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new EnergyVar(1);
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            int energyGain = DynamicVars.Energy.IntValue;
            await PlayerCmd.GainEnergy(energyGain, Owner);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Energy.UpgradeValueBy(1m);
        }
    }
}