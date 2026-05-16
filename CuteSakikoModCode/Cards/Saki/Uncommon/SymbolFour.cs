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
    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            // 当前累积的总压力值（初始4，升级后变8）
            yield return new BlockVar("TotalPressure", 4m, ValueProp.Move);
            // 每次打出增加的压力值（未升级2，升级后4），使用 BlockVar 以享受格挡预览
            yield return new BlockVar("ExtraPressure", 2m, ValueProp.Move);
        }
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
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
        var totalPressureVar = (BlockVar)DynamicVars["TotalPressure"];
        var extraPressureVar = DynamicVars["ExtraPressure"];
        var currentTotal = totalPressureVar.BaseValue;

        // 给予目标当前累积的总压力
        await PowerCmd.Apply<PressurePower>(choiceContext, target, (int)currentTotal, Owner.Creature, this);

        // 获得等量格挡（此处作为 BlockVar 传入 GainBlock，会自动享受格挡加成）
        await CreatureCmd.GainBlock(Owner.Creature, new BlockVar(currentTotal, ValueProp.Move), cardPlay);

        // 累加基础增量，避免双重加成（累积值 TotalPressure 下次打出时会被完整加成）
        totalPressureVar.UpgradeValueBy(extraPressureVar.BaseValue);
    }

    protected override void OnUpgrade()
    {
        // 基础总压力 4 → 8
        ((BlockVar)DynamicVars["TotalPressure"]).UpgradeValueBy(4m);
        // 每次增量 2 → 4
        ((BlockVar)DynamicVars["ExtraPressure"]).UpgradeValueBy(2m);
    }
}