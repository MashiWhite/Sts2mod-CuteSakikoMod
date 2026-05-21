using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class HandleTheTeamOutfits() : CuteAnonCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string ChordId => "AnonEChord";
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust,CutesakiKeywords.NoNote.GetModKeywordCardKeyword(), CutesakiKeywords.Chord.GetModKeywordCardKeyword()];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (ChordManager.AllChords.TryGetValue("AnonEChord", out var def))
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
        if (currentMinor == "AnonEChord")
            await guitar.AddChordToStored(choiceContext, "AnonEChord");
        else
            guitar.TempReplaceChord(ChordCategory.Minor, "AnonEChord");
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 2 → 1
        AddKeyword(CardKeyword.Innate);
    }
}