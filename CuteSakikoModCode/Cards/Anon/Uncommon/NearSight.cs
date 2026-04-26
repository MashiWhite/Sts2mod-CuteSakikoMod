
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Status;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class NearSight() : CuteAnonCard(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
     
        
        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                yield return HoverTipFactory.FromPower<NearSightPower>();
                yield return HoverTipFactory.FromPower<CloseObservePower>();
                yield return HoverTipFactory.FromCard<CloseObserve>(IsUpgraded);
            }
        }
        
        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();
            var power = await PowerCmd.Apply<NearSightPower>(choiceContext, Owner.Creature, 1, Owner.Creature, this);
            if (power != null && IsUpgraded)
                power.SetUpgraded(true);
        }

        protected override void OnUpgrade()
        {
            EnergyCost.UpgradeBy(-1); // 2 → 1
        }
    }
}