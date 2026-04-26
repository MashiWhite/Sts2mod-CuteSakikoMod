using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon
{
    public class EasyNow() : CuteAnonCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
        private bool _targetAll = false;

        public override TargetType TargetType => _targetAll ? TargetType.AllEnemies : TargetType.AnyEnemy;

        public override IEnumerable<CardKeyword> CanonicalKeywords
        {
            get { yield return CardKeyword.Exhaust; }
        }

        protected override IEnumerable<DynamicVar> CanonicalVars
        {
            get
            {
                yield return new PowerVar<StrengthPower>(2m);
            }
        }

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            TriggerBanter();

            int strengthLoss = DynamicVars["StrengthPower"].IntValue;

            if (_targetAll)
            {
                // 对所有敌人施加负力量
                var enemies = Owner.Creature.CombatState?.Enemies;
                if (enemies != null)
                {
                    foreach (var enemy in enemies)
                        await PowerCmd.Apply<StrengthPower>(choiceContext,enemy, -strengthLoss, Owner.Creature, this);
                }
            }
            else
            {
                // 对单个目标施加负力量
                if (cardPlay.Target == null) return;
                await PowerCmd.Apply<StrengthPower>(choiceContext,cardPlay.Target, -strengthLoss, Owner.Creature, this);
            }
        }

        protected override void OnUpgrade()
        {
            DynamicVars["StrengthPower"].UpgradeValueBy(1m);
            // 目标变为所有敌人
            _targetAll = true;
        }
    }
}