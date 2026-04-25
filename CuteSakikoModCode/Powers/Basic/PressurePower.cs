using BaseLib.Hooks;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;   // 新增：提供 ThrowingPlayerChoiceContext
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Basic;

public sealed class PressurePower : CuteSakikoModPower
{
    public PressurePower()
    {
        DisplayAmountChanged += OnDisplayAmountChanged;
    }
    
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { };

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BreakDownPower>(); }
    }

    private void OnDisplayAmountChanged()
    {
        if (Amount <= 0 && IsMutable)
            TaskHelper.RunSafely(PowerCmd.Remove(this));
    }

    // 生命条覆盖：从左向右增长
    public override IEnumerable<HealthBarForecastSegment> GetHealthBarForecastSegments(HealthBarForecastContext context)
    {
        if (Owner == null || Owner.MaxHp <= 0 || Amount <= 0)
            return Enumerable.Empty<HealthBarForecastSegment>();

        var pressureAmount = Amount;
        var segment = new HealthBarForecastSegment(
            pressureAmount,
            new Color(1f, 1f, 0f, 0.8f),
            HealthBarForecastDirection.FromLeft
        );
        return new[] { segment };
    }

    // ********** 修复签名：添加 PlayerChoiceContext 参数 **********
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,   // 新参数
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power != this) return;
        
        // 压力增加时，提升自己手牌中骑士之剑的伤害
        if (amount > 0 && Owner != null && Owner.IsPlayer && CombatState != null)
        {
            int delta = (int)amount;
            if (delta <= 0) return;
            var player = Owner.Player;
            var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust };
            foreach (var pileType in piles)
            {
                var pile = pileType.GetPile(player);
                if (pile == null) continue;
                foreach (var card in pile.Cards)
                {
                    if (card is KnightSword ks)
                    {
                        ks.DynamicVars.Damage.BaseValue += delta;
                    }
                }
            }
        }

        await CheckAndTriggerCollapse(choiceContext);   // 传入上下文
    }

    // 生命值减少时检测
    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner) return;
        if (delta >= 0) return; // 只关心生命值减少
        // 没有 PlayerChoiceContext，使用 ThrowingPlayerChoiceContext
        await CheckAndTriggerCollapse(new ThrowingPlayerChoiceContext());
    }

    // 更新该方法，添加 PlayerChoiceContext 参数
    private async Task CheckAndTriggerCollapse(PlayerChoiceContext ctx)
    {
        if (Owner == null || !Owner.IsAlive) return;
        if (Amount >= Owner.CurrentHp)
        {
            // 清除所有压力
            await PowerCmd.ModifyAmount(ctx, this, -Amount, Owner, null);
            // 获得一层崩溃
            await PowerCmd.Apply<BreakDownPower>(ctx, Owner, 1, Owner, null);
        }
    }

    // 伤害加成（这个方法是正确的，无需修改）
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner != target) return 0m;
        return amount * (Amount / 100m);
    }
}