
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common
{
    public class SeekingTarget() : CuteAnonCard(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new DamageVar(8m, ValueProp.Move);
                yield return new PowerVar<VulnerablePower>(1m);
            }
        }
        
        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                yield return HoverTipFactory.FromPower<VulnerablePower>();
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            var combat = Owner.Creature.CombatState;
            if (combat == null) return;

            int damage = DynamicVars.Damage.IntValue;
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .TargetingAllOpponents(combat)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            int vulnAmount = DynamicVars["VulnerablePower"].IntValue;
            foreach (var enemy in combat.Enemies.Where(e => e.IsAlive))
            {
                await PowerCmd.Apply<VulnerablePower>(choiceContext,enemy, vulnAmount, Owner.Creature, this);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Damage.UpgradeValueBy(4m); // 8 → 12
            DynamicVars["VulnerablePower"].UpgradeValueBy(1m); // 1 → 2
        }
    }
}