using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;


namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Common;

public class WhyNoHelp : CuteSakikoModCard
{
    public WhyNoHelp() : base(3, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(25m, ValueProp.Move);
            yield return new EnergyVar(2);
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 造成伤害
        var results = await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this,cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 检查是否击杀了敌人
        var killed = results.Results
            .SelectMany(r => r)
            .Any(r => r.WasTargetKilled);

        if (killed)
        {
            await PlayerCmd.GainEnergy((decimal)DynamicVars.Energy.IntValue, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m); // 25 → 28
        DynamicVars.Energy.UpgradeValueBy(1m); // 2 → 3
    }
}