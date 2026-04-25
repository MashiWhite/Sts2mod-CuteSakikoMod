
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class ThisHard() : CuteAnonCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
        public override string ChordId => "AnonFChord";
        
        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                yield return CardKeyword.Exhaust;
                yield return CutesakiKeywords.NoNote; // 不产生音符
                yield return CutesakiKeywords.Chord;
            }
        }

        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                if (ChordManager.AllChords.TryGetValue("AnonFChord", out var def))
                {
                    string condition = def.GetConditionText();
                    string effectDesc = ChordDisplayHelper.GetFormattedDescription(def, 1);
                    string fullDesc = $"{condition}\n{effectDesc}";
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

            var currentMajor = guitar.GetCurrentChords().GetValueOrDefault(ChordCategory.Major);
            if (currentMajor == "AnonFChord")
            {
                await guitar.AddChordToStored(choiceContext, "AnonFChord");
            }
            else
            {
                guitar.TempReplaceChord(ChordCategory.Major, "AnonFChord");
            }
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1);
            AddKeyword(CardKeyword.Innate);
        }
    }
}