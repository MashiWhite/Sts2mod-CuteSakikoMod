using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class LookCchord() : CuteAnonCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    private const string MyChordId = "AnonCChord";

    public override string ChordId => MyChordId;

    public override IEnumerable<CardKeyword> CanonicalKeywords =>
    [
        CardKeyword.Exhaust, CutesakiKeywords.NoNote.GetModCardKeyword(), CutesakiKeywords.Chord.GetModCardKeyword()
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (ChordManager.AllChords.TryGetValue(ChordId, out var def))
            {
                var condition = def.GetConditionText();

                // ★ 这张卡自身升级时，和弦战斗内 +1
                // GetFormattedDescription 会用 def.BaseValues[i] + bonus 替换 {0} {1}...
                // 每张卡使用自己对应的 BaseValues
                var bonus = IsUpgraded ? 1 : 0;

                var effectDesc = ChordDisplayHelper.GetFormattedDescription(def, bonus);
                var fullDesc = $"{condition}\n{effectDesc}";
                var title = new LocString("card_keywords", def.TitleKey);
                yield return new HoverTip(title, fullDesc);
            }
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var chords = Owner.GetChords();
        if (chords == null) return;

        var temporaryChords = chords.GetTemporaryChords();
        if (temporaryChords.Contains(ChordId))
            await ChordNoteSystem.PlayChordAsync(Owner, MyChordId, choiceContext);
        else
            chords.AddTemporaryChord(ChordId);

        var sfxPath = AudioManager.ResolveAudioPath("look_cchord.mp3");
        AudioManager.PlaySound(sfxPath);
    }

    protected override void OnUpgrade()
    {
    }
}