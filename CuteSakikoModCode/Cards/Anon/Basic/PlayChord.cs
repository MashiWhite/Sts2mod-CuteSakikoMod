using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Ancient;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Basic;

[RegisterArchaicToothTranscendence(typeof(PlayImmediately))]
[RegisterCharacterStarterCard(typeof(CuteAnon))]
public class PlayChord() : CuteAnonCard(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.NoNote.GetModKeywordCardKeyword()];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var relic = Owner.Relics.FirstOrDefault(r => r is AnonGuitar) as AnonGuitar;
        if (relic == null) return;

        await relic.TriggerAllStoredChords(choiceContext);
    }

    public CardModel GetTranscendenceTransformedCard()
    {
        return ModelDb.Card<PlayImmediately>();
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Retain);
    }
}