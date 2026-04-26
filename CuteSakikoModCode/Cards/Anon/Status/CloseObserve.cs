
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status
{
    [Pool(typeof(StatusCardPool))]
    public class CloseObserve() : CustomCardModel(1, CardType.Status, CardRarity.Status, TargetType.AnyEnemy)
    {
        
        public override string CustomPortraitPath =>
            (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

        public override string PortraitPath =>
            (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

        public override string BetaPortraitPath =>
            (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();
        
        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                yield return CardKeyword.Ethereal;
                yield return CardKeyword.Exhaust;
            }
        }
        
        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                yield return HoverTipFactory.FromPower<CloseObservePower>();
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;
            await PowerCmd.Apply<CloseObservePower>(choiceContext, cardPlay.Target, 1, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1); // 1 → 0
        }
    }
}