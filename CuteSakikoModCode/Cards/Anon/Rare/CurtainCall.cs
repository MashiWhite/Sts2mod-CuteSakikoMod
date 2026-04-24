
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare
{
    public class CurtainCall() : CuteAnonCard(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get
            {
                yield return CardKeyword.Exhaust;
            }
        }
        
        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new DamageVar(8m, ValueProp.Move);
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);
            TriggerBanter();

            var damage = DynamicVars.Damage.BaseValue;
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(5m); // 8 → 13
        }
    }
}