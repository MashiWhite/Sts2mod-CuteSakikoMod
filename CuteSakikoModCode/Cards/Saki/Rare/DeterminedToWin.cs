
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
            yield return new DamageVar(5m, ValueProp.Move);
            yield return new PowerVar<PressurePower>(2m);
        }
    }

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        int x = ResolveEnergyXValue(); // 获取X值（消耗的能量）
        int hits = x + 1; // 攻击次数 = X+1
        if (hits <= 0) return;

        int step = IsUpgraded ? 2 : 1;
        int baseDamage = 5;
        int basePressure = 2;

        for (int i = 0; i < hits; i++)
        {
            int damage = baseDamage + i * step;
            int pressure = basePressure + i * step;

            // 造成伤害
            await DamageCmd.Attack(damage)
                .FromCard(this)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);

            // 给予压力
            if (pressure > 0)
            {
                await PowerCmd.Apply<PressurePower>(choiceContext,cardPlay.Target, pressure, Owner.Creature, this);
            }
        }
    }

    protected override void OnUpgrade()
    {
        // 升级效果：步长从1变为2（已在逻辑中处理）
    }
}