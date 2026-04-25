
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Uncommon;


public class SymbolFour() : CuteSakikoModCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.AnyEnemy)
{
    // 动态变量：总压力（初始值，升级后更新）
    protected override IEnumerable<DynamicVar> CanonicalVars =>
    [
        new DamageVar("TotalPressure", IsUpgraded ? 8m : 4m, ValueProp.Move)
    ];

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<PressurePower>();
            yield return HoverTipFactory.FromPower<BreakDownPower>();
        }
    }

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

        var target = cardPlay.Target;

        // 获取当前总压力值（初始值）
        var totalPressureVar = (DamageVar)DynamicVars["TotalPressure"];
        var currentTotal = totalPressureVar.BaseValue;

        // 计算本次应给予的压力：基础 + 累积（累积就是当前总压力减去初始值）
        var basePressure = IsUpgraded ? 8m : 4m;
        var bonus = currentTotal - basePressure;
        var totalPressure = currentTotal;

        // 给予目标压力
        await PowerCmd.Apply<PressurePower>(choiceContext,target, (int)totalPressure, Owner.Creature, this);

        // 获得等量格挡
        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(totalPressure, ValueProp.Move), cardPlay);

        // 增加累积值（这里使用 UpgradeValueBy 来增加总压力，而非直接赋值）
        var increment = IsUpgraded ? 4m : 2m;
        totalPressureVar.UpgradeValueBy(increment);
    }

    protected override void OnUpgrade()
    {
        // 升级：基础压力从4变为8，总压力变量也需要升级
        var totalPressureVar = (DamageVar)DynamicVars["TotalPressure"];
        totalPressureVar.UpgradeValueBy(4m); // 4 → 8
        // 注意：累积值保留，因为升级后总压力自动增加了4，但累积值（bonus）不变
    }
}