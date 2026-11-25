using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Compat;

/// <summary>
///     攻击段数变量的跨版本工厂。
///     0.113+ 用 HitCountVar / CalculatedHitCountVar，让卡面预览经过
///     ModifyAttackHitCount 钩子；0.107 / 0.111 没有这些类，退回
///     普通 RepeatVar / 自定义 DynamicVar。
///     变量名保留原 key，三语描述不用改。
/// </summary>
internal static class AttackHitCountVar
{
    public static DynamicVar Create(int hits) => Create("Repeat", hits);

    public static DynamicVar Create(string name, int hits)
    {
#if STS2_AT_LEAST_0_113_0
        return new HitCountVar(name, hits);
#else
        return new RepeatVar(name, hits);
#endif
    }

    /// <summary>
    ///     动态总段数 = 1 + multiplier。调用方仍需提供
    ///     CalculationBaseVar(1) / CalculationExtraVar(1)。
    /// </summary>
    public static DynamicVar CreateCalculated(
        string name,
        Func<CardModel, Creature?, decimal> multiplier)
    {
#if STS2_AT_LEAST_0_113_0
        return new CalculatedHitCountVar(name).WithMultiplier(multiplier);
#else
        return new LegacyCalculatedHitCountVar(name, multiplier);
#endif
    }

#if !STS2_AT_LEAST_0_113_0
    private sealed class LegacyCalculatedHitCountVar(
        string name,
        Func<CardModel, Creature?, decimal> multiplier)
        : DynamicVar(name, 1m)
    {
        public override void UpdateCardPreview(
            CardModel card,
            CardPreviewMode previewMode,
            Creature? target,
            bool runGlobalHooks)
        {
            base.UpdateCardPreview(card, previewMode, target, runGlobalHooks);
            EnchantedValue = BaseValue;
            if (card.Owner == null) return;
            PreviewValue = 1m + multiplier(card, target);
        }
    }
#endif
}

