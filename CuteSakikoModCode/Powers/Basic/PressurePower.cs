
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Combat.HealthBars;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Basic;

public sealed class PressurePower : CuteSakikoModPower, IHealthBarForecastSource
{
    public PressurePower()
    {
        DisplayAmountChanged += OnDisplayAmountChanged;
    }

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { };

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [HoverTipFactory.FromPower<BreakDownPower>()];

    public IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
    {
        if (Owner == null || Owner.MaxHp <= 0 || Amount <= 0)
            return Enumerable.Empty<HealthBarForecastSegment>();

        var pressureAmount = Amount;
        var segment = new HealthBarForecastSegment(
            pressureAmount,
            new Color(1f, 1f, 0f, 0.8f),
            HealthBarForecastGrowthDirection.FromLeft
        );
        return new[] { segment };
    }

    private void OnDisplayAmountChanged()
    {
        if (Amount <= 0 && IsMutable)
            TaskHelper.RunSafely(PowerCmd.Remove(this));
    }

    // 压力层数变化时只处理崩溃检查
    // 翻倍与骑士之剑增伤已迁移到 SwordManager / MasqueradeRhapsody
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this) return;
        await CheckAndTriggerCollapse(choiceContext);
    }

    // 受伤时检查崩溃（兼容玩家与敌人）
    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner) return;
        if (delta >= 0) return;          // 只关心受伤
        if (Owner == null || CombatState == null) return;

        // 使用 CombatState 的第一个玩家作为 Owner 构造合法上下文
        var ownerPlayer = CombatState.Players[0];
        var ctx = new HookPlayerChoiceContext(ownerPlayer, ownerPlayer.NetId, GameActionType.Combat);

        Task task = CheckAndTriggerCollapse(ctx);
        await ctx.AssignTaskAndWaitForPauseOrCompletion(task);
    }

    private async Task CheckAndTriggerCollapse(PlayerChoiceContext ctx)
    {
        if (Owner == null || !Owner.IsAlive) return;
        if (Amount >= Owner.CurrentHp)
        {
            await PowerCmd.ModifyAmount(ctx, this, -Amount, Owner, null);
            await PowerCmd.Apply<BreakDownPower>(ctx, Owner, 1, Owner, null);
        }
    }

    public override decimal ModifyDamageAdditive(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        if (Owner != target) return 0m;
        return amount * (Amount / 100m);
    }
}