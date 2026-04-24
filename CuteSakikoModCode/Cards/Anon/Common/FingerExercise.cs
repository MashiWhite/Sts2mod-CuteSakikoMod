
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common
{
    public class FingerExercise() : CuteAnonCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                // 未升级时的基础伤害为3
                yield return new DamageVar(3m, ValueProp.Move);
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Target == null) return;

            TriggerBanter();

            int hitCount = 3; // 固定攻击3次
            var damage = DynamicVars.Damage.BaseValue;

            for (int i = 0; i < hitCount; i++)
            {
                await DamageCmd.Attack(damage)
                    .FromCard(this)
                    .Targeting(cardPlay.Target)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);
            }
        }

        protected override void OnUpgrade()
        {
            // 升级后基础伤害提升至4
            DynamicVars.Damage.UpgradeValueBy(1m);
        }
    }
}