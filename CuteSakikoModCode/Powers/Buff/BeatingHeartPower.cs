using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class BeatingHeartPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // 注册 "Cap" 变量，供 powers.json 里的 {Cap} 使用
    protected override IEnumerable<DynamicVar> CanonicalVars => new[]
    {
        new DynamicVar("Cap", 0m)
    };

    protected override object? InitInternalData() => new Data();

    private class Data
    {
        public decimal DamageReceivedThisTurn;
    }

    /// <summary>上限 = 最大生命值 × 十日终焉层数 ÷ 10（每层 = 10% 血量）。</summary>
    private decimal GetCap()
    {
        var endPower = Owner.GetPower<TenDayEndPower>();
        int days = endPower?.Amount ?? 0;
        return Owner.MaxHp * days / 10m;
    }

    /// <summary>
    /// 刷新 Cap 动态变量。
    /// 调用时机：① 首次挂上（AfterApplied）② 每回合开始（BeforeSideTurnStart）
    /// ③ 十日终焉层数变化（MovementNotFinishedPower.AfterPowerAmountChanged 转发）。
    /// </summary>
    public void UpdateCap()
    {
        if (!IsMutable) return;   // 图鉴/预览时为 Canonical，碰 Owner 会抛
        DynamicVars["Cap"].BaseValue = GetCap();
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        UpdateCap();
        return Task.CompletedTask;
    }

    public override Decimal ModifyHpLostBeforeOstyLate(
        Creature target,
        Decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || amount <= 0) return amount;

        var data = GetInternalData<Data>();
        var cap = GetCap();
        var remaining = Math.Max(0, cap - data.DamageReceivedThisTurn);
        return Math.Min(amount, remaining);
    }

    public override Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || result.WasFullyBlocked) return Task.CompletedTask;

        var data = GetInternalData<Data>();
        data.DamageReceivedThisTurn += result.UnblockedDamage;
        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnStart(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        GetInternalData<Data>().DamageReceivedThisTurn = 0;
        UpdateCap();
        return Task.CompletedTask;
    }
}