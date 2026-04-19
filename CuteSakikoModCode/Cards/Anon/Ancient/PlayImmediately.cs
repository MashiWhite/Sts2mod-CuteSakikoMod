
using BaseLib.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;



namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Ancient
{
    public class PlayImmediately : CuteAnonCard
    {
        public PlayImmediately() : base(2, CardType.Power, CardRarity.Ancient, TargetType.Self)
        {
        }

        public override string PortraitPath =>
            (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

        public override IEnumerable<CardKeyword> CanonicalKeywords => new[] { CardKeyword.Innate }; // 可选

        
        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                yield return HoverTipFactory.FromPower<PlayImmediatelyPower>();
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();
            await PowerCmd.Apply<PlayImmediatelyPower>(Owner.Creature, 1, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1); 
        }
    }
}