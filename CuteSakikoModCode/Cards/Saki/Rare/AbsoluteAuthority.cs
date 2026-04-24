
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare
{
 
    public class AbsoluteAuthority() : CuteSakikoModCard(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new DamageVar(30m, ValueProp.Move);
                yield return new PowerVar<PressurePower>(10m);
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            ArgumentNullException.ThrowIfNull(cardPlay.Target);

            var damage = DynamicVars.Damage.BaseValue;
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            int pressureAmount = DynamicVars["PressurePower"].IntValue;
            await PowerCmd.Apply<PressurePower>(cardPlay.Target, pressureAmount, Owner.Creature, this);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(10m); // 30 → 40
            DynamicVars["PressurePower"].UpgradeValueBy(5m); // 10 → 15
        }
    }
}