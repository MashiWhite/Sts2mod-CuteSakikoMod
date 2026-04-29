using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;

// ReSharper disable once InconsistentNaming
public class AIHeart() : CuteAnonCard(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
{
    public override string ChordId => "GreyAnonChord";

    protected override IEnumerable<string> RegisteredKeywordIds =>
        [CutesakiKeywords.NoNote, CutesakiKeywords.Chord, CutesakiKeywords.OtherAnon];

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];


    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (ChordManager.AllChords.TryGetValue("GreyAnonChord", out var def))
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

        var currentMinor = guitar.GetCurrentChords().GetValueOrDefault(ChordCategory.Minor);
        if (currentMinor == "GreyAnonChord")
            await guitar.AddChordToStored(choiceContext, "GreyAnonChord");
        else
            guitar.TempReplaceChord(ChordCategory.Minor, "GreyAnonChord");
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        AddKeyword(CardKeyword.Innate);
    }
}