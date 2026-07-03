using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Hooks;
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

    // 压力增加时，提升骑士之剑伤害（已有合法上下文）
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this) return;

        if (amount > 0 && Owner != null && Owner.IsPlayer && CombatState != null)
        {
            var delta = (int)amount;
            if (delta <= 0) return;
            var player = Owner.Player;
            var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust };
            foreach (var pileType in piles)
            {
                var pile = pileType.GetPile(player);
                if (pile == null) continue;
                foreach (var card in pile.Cards)
                    if (card is KnightSword ks)
                        ks.DynamicVars.Damage.BaseValue += delta;
            }
        }

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