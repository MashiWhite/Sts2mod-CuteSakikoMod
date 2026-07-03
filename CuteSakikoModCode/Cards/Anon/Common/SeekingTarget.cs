using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Common;

public class SeekingTarget() : CuteAnonCard(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(4m, ValueProp.Move);
            yield return new PowerVar<VulnerablePower>(2m);
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<VulnerablePower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        TriggerBanter();

        var combat = Owner.Creature.CombatState;
        if (combat == null) return;

        var damage = DynamicVars.Damage.IntValue;
        await DamageCmd.Attack(damage)
            .FromCard(this,cardPlay)
            .TargetingAllOpponents(combat)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        var vulnAmount = DynamicVars["VulnerablePower"].IntValue;
        foreach (var enemy in combat.Enemies.Where(e => e.IsAlive))
            await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy, vulnAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m); 
    }
}