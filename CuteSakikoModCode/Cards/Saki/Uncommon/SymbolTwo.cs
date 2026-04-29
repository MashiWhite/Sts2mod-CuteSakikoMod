using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;

public class SymbolTwo() : CuteSakikoModCard(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 动态变量：伤害值（基础1，升级2）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar(1m, ValueProp.Move)
    ];

    // 悬停提示：显示压力能力
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

    // 高亮特效：当目标有压力时卡牌发光（简化：只要场上有敌人有压力就高亮）
    protected override bool ShouldGlowGoldInternal
    {
        get
        {
            if (CombatState == null) return false;
            foreach (var enemy in CombatState.HittableEnemies)
            {
                var pressure = enemy.GetPower<PressurePower>();
                if (pressure != null && pressure.Amount > 0)
                    return true;
            }

            return false;
        }
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Target == null) return;

        // 获取目标压力层数
        var pressure = cardPlay.Target.GetPower<PressurePower>();
        var layers = pressure?.Amount ?? 0;

        if (layers <= 0) return;

        // 伤害值（未升级2，升级3）
        var damage = DynamicVars.Damage.BaseValue;

        // 对同一目标造成 layers 次 damage 伤害
        await DamageCmd.Attack(damage)
            .FromCard(this)
            .Targeting(cardPlay.Target)
            .WithHitCount(layers)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        // 升级：伤害增加1点（1→2）
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}