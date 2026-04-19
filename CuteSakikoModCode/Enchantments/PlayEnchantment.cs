using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Enchantments
{
    public sealed class PlayEnchantment : CustomEnchantmentModel
    {
        public override bool ShowAmount => false;
        public override bool HasExtraCardText => true;

        protected override string? CustomIconPath =>
            "CuteSakikoMod/images/enchantments/play.png";

        public override bool CanEnchant(CardModel card) => true;

        public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
        {
            await base.OnPlay(choiceContext, cardPlay);
            if (Status != EnchantmentStatus.Normal) return;

            var guitar = Card.Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
            if (guitar != null)
            {
                await guitar.TriggerLastStoredChord(choiceContext);
            }
        }
    }
}