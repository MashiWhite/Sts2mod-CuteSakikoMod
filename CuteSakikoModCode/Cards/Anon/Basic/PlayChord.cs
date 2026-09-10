using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Ancient;
using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Basic;

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

        // 播放所有储存的和弦，并清空储存和弦与音符
        await ChordNoteSystem.PlayAllStoredChordsAsync(Owner, choiceContext);
        ChordNoteSystem.ClearNotes(Owner); // 原 TriggerAllStoredChords 会清空音符
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