
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare
{
    public class PerfectPlay : CuteAnonCard
    {
        public PerfectPlay() : base(2, CardType.Skill, CardRarity.Rare, TargetType.Self)
        {
        }

        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                yield return CutesakiKeywords.NoNote;
                yield return CardKeyword.Exhaust;

            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();
            var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
            if (guitar == null) return;

            await guitar.TriggerAllLearnedChords(choiceContext);
        }

        protected override void OnUpgrade()
        {
            AddKeyword(CardKeyword.Innate);
        }
    }
}