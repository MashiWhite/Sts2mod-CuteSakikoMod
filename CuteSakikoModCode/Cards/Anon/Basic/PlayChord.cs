using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Ancient;
using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Basic;
//[RegisterCharacterStarterCard(typeof(CuteRana), Order = 3)]
[RegisterArchaicToothTranscendence(typeof(PlayImmediately))]
[RegisterCharacterStarterCard(typeof(CuteAnon), Order = 2)]
public class PlayChord() : CuteAnonCard(0, CardType.Skill, CardRarity.Basic, TargetType.Self)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CutesakiKeywords.NoNote.GetModCardKeyword()];

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