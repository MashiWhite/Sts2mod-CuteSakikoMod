using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;

public class Hekitenbansou() : CuteAnonCard(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
{
    public override string ChordId => "HekitenbansouChord";
    
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust,CutesakiKeywords.NoNote.GetModKeywordCardKeyword(), CutesakiKeywords.Chord.GetModKeywordCardKeyword()];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (ChordManager.AllChords.TryGetValue("HekitenbansouChord", out var def))
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

        const string chordId = "HekitenbansouChord";

        // 若临时槽中还未拥有该和弦，则添加临时槽位；否则直接储存一个和弦
        var temporaryChords = guitar.GetTemporaryChords(); // 需公开此方法，见下方说明
        if (temporaryChords.Contains(chordId))
            await guitar.AddChordToStored(choiceContext, chordId);
        else
            guitar.AddTemporaryChord(chordId);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1); // 3 → 2
        AddKeyword(CardKeyword.Innate);
    }
}