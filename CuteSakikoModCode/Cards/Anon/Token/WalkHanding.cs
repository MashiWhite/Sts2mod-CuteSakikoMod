
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Token
{
    [Pool(typeof(TokenCardPool))]
    public class WalkHanding : CustomCardModel
    {
        public WalkHanding() : base(0, CardType.Power, CardRarity.Token, TargetType.AnyAlly)
        {
        }

        public override string PortraitPath =>
            (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").CardImagePath();

        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new PowerVar<WalkHandingPower>(8m);
            }
        }

        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                yield return HoverTipFactory.FromPower<EscapismTendencyPower>();
                yield return HoverTipFactory.FromPower<WalkHandingPower>();
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            var target = cardPlay.Target;
            if (target == null) return;

            var escapismPower = target.GetPower<EscapismTendencyPower>();
            if (escapismPower == null) return;

            await PowerCmd.Remove(escapismPower);

            int amount = DynamicVars["WalkHandingPower"].IntValue;
            await PowerCmd.Apply<WalkHandingPower>(target, amount, Owner.Creature, this);
            await PowerCmd.Apply<WalkHandingPower>(Owner.Creature, amount, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars["WalkHandingPower"].UpgradeValueBy(5m); // 8 → 13
        }
    }
}