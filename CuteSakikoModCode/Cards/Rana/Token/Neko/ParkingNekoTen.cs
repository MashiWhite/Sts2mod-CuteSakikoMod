using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Token.Neko
{
    public class ParkingNekoTen : NekoCard
    {
        public ParkingNekoTen() : base(0, CardType.Attack, CardRarity.Token, TargetType.RandomEnemy) { }

        protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
        {
            new DamageVar(3m, ValueProp.Move),
            new RepeatVar(3)
        };

        protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            int times = DynamicVars.Repeat.IntValue;
            await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
                .WithHitCount(times)
                .FromCard(this,cardPlay)
                .TargetingRandomOpponents(CombatState)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        protected override void OnUpgrade()
        {
            DynamicVars.Repeat.UpgradeValueBy(2);
        }
    }
}