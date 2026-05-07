using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

public class Osusume() : CuteAnonCard(3, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string ChordId => "AnonDChord";

    protected override IEnumerable<string> RegisteredKeywordIds => [CutesakiKeywords.NoNote, CutesakiKeywords.Chord];
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (ChordManager.AllChords.TryGetValue("AnonDChord", out var def))
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
        if (currentDominant == "AnonDChord")
            await guitar.AddChordToStored(choiceContext, "AnonDChord");
        else
            guitar.TempReplaceChord(ChordCategory.Dominant, "AnonDChord");
    }

    protected override void OnUpgrade()
    {
        AddKeyword(CardKeyword.Innate);
    }
}