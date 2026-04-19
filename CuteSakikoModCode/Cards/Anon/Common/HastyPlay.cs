
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common
{
    public class HastyPlay : CuteAnonCard
    {
        public HastyPlay() : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
        {
        }

        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new DamageVar(6m, ValueProp.Move);
                yield return new CardsVar(1);
            }
        }

        protected override bool ShouldGlowGoldInternal
        {
            get
            {
                var lastNote = MusicNoteManager.GetLastNote(Owner);
                return lastNote == CardType.Attack;
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            if (cardPlay.Target != null)
                await CreatureCmd.Damage(choiceContext, cardPlay.Target, DynamicVars.Damage, this);

            await CardPileCmd.Draw(choiceContext, DynamicVars.Cards.BaseValue, Owner);
        }

        public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            await base.AfterCardPlayed(choiceContext, cardPlay);

            if (cardPlay.Card.Owner != Owner) return;

            UpdateCostBasedOnLastNote();
        }

        public override async Task AfterCardEnteredCombat(CardModel card)
        {
            await base.AfterCardEnteredCombat(card);

            if (card != this) return;

            UpdateCostBasedOnLastNote();
        }

        private void UpdateCostBasedOnLastNote()
        {
            var lastNote = MusicNoteManager.GetLastNote(Owner);
            if (lastNote == CardType.Attack)
                EnergyCost.SetThisTurn(0);
            else
                EnergyCost.SetThisTurn(1); // 恢复原费用
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(3m);   // 6 → 9
            DynamicVars.Cards.UpgradeValueBy(1m);    // 1 → 2
        }
    }
}