using CuteSakikoMod.CuteSakikoModCode.Cards.Mod;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status;

public class LayFlat() : ModStatusCard(1, CardType.Status, CardRarity.Status, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust, CardKeyword.Retain];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Retain);
        AddKeyword(CardKeyword.Ethereal);
    }
}