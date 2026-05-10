using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Audio;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class LookCchord() : CuteAnonCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
{
    public override string ChordId => "AnonCChord";
    protected override IEnumerable<string> RegisteredKeywordIds => [CutesakiKeywords.NoNote, CutesakiKeywords.Chord];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (ChordManager.AllChords.TryGetValue("AnonCChord", out var def))
            {
                var condition = def.GetConditionText();
                var effectDesc = ChordDisplayHelper.GetFormattedDescription(def, 1);
                var fullDesc = $"{condition}\n{effectDesc}";
                var title = new LocString("card_keywords", def.TitleKey);
                yield return new HoverTip(title, fullDesc);
            }
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) return;

        var currentDominant = guitar.GetCurrentChords().GetValueOrDefault(ChordCategory.Dominant);
        if (currentDominant == "AnonCChord")
            await guitar.AddChordToStored(choiceContext, "AnonCChord");
        else
            guitar.TempReplaceChord(ChordCategory.Dominant, "AnonCChord");
        
        // 播放特定和弦音效
        var sfxPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "audio", "look_cchord.mp3");
        AssetHelper.AudioManager.PlaySound(sfxPath, 1.0f); // 1.0 是基础音量
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}