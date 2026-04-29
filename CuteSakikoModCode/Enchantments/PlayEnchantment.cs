using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Enchantments;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Enchantments;

[RegisterEnchantment]
public sealed class PlayEnchantment : ModEnchantmentTemplate
{
    public override bool ShowAmount => false;
    public override bool HasExtraCardText => true;

    public override EnchantmentAssetProfile AssetProfile => new("CuteSakikoMod/images/enchantments/play.png");


    public override bool CanEnchant(CardModel card)
    {
        return true;
    }

    public override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
    {
        await base.OnPlay(choiceContext, cardPlay);
        if (Status != EnchantmentStatus.Normal) return;

        var guitar = Card.Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar != null) await guitar.TriggerLastStoredChord(choiceContext);
    }
}