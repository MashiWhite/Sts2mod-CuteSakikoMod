using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common
{
    public class LookCChord() : CuteAnonCard(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
        public override string ChordId => "AnonCChord";

        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                yield return CardKeyword.Exhaust;
                yield return CutesakiKeywords.NoNote; // 不产生音符，避免意外匹配
                yield return CutesakiKeywords.Chord;
            }
        }

        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                if (ChordManager.AllChords.TryGetValue("AnonCChord", out var def))
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

            var currentDominant = guitar.GetCurrentChords().GetValueOrDefault(ChordCategory.Dominant);
            if (currentDominant == "AnonCChord")
            {
                await guitar.AddChordToStored(choiceContext, "AnonCChord");
            }
            else
            {
                guitar.TempReplaceChord(ChordCategory.Dominant, "AnonCChord");
            }
        }

        protected override void OnUpgrade()
        {
            AddKeyword(CardKeyword.Innate);
        }
    }
}