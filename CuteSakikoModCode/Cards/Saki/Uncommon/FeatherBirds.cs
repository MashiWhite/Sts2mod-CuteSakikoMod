using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class FeatherBirds() : CuteSakikoModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
{
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(10m, ValueProp.Move),
        new PowerVar<PressurePower>(5m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var damage = DynamicVars["Damage"].IntValue;
        var pressureAmount = DynamicVars["PressurePower"].IntValue;

        // 对所有敌人造成伤害并施加压力
        var enemies = CombatState?.HittableEnemies;
        if (enemies != null)
            foreach (var enemy in enemies)
            {
                // 造成伤害
                await DamageCmd.Attack(damage)
                    .FromCard(this)
                    .Targeting(enemy)
                    .WithHitFx("vfx/vfx_attack_slash")
                    .Execute(choiceContext);

                // 施加压力
                await PowerCmd.Apply<PressurePower>(choiceContext, enemy, pressureAmount, Owner.Creature, this);
            }

        // 自身获得压力
        await PowerCmd.Apply<PressurePower>(choiceContext, Owner.Creature, pressureAmount, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        // 升级：伤害 7 → 10，压力 5 → 8
        DynamicVars["Damage"].UpgradeValueBy(3m);
        DynamicVars["PressurePower"].UpgradeValueBy(3m);
    }
}