using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Rare;

public class DeterminedToWin() : CuteSakikoModCard(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
{
    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DamageVar(5m, ValueProp.Move);                   // 基础伤害
            yield return new PowerVar<PressurePower>(2m);                    // 基础压力
            yield return new DamageVar("ExtraDamage", 2m, ValueProp.Move);   // 每次递增的伤害与压力量
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<PressurePower>(); }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        var x = ResolveEnergyXValue();
        var hits = x + 1;
        if (hits <= 0) return;

        var baseDamage = DynamicVars.Damage.BaseValue;
        var basePressure = 2; // 基础压力固定为2，未升级不变
        var step = (int)((DamageVar)DynamicVars["ExtraDamage"]).BaseValue;

        for (var i = 0; i < hits; i++)
        {
            var damage = baseDamage + i * step;
            var pressure = basePressure + i * step;

            // 造成伤害
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            // 给予压力
            if (pressure > 0)
                await PowerCmd.Apply<PressurePower>(choiceContext, cardPlay.Target, pressure, Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        // 步长从 2 提升至 4
        ((DamageVar)DynamicVars["ExtraDamage"]).UpgradeValueBy(2m);
    }
}