using BaseLib.Abstracts;
using BaseLib.Hooks;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using StringExtensions = BaseLib.Extensions.StringExtensions;

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

    // 压力变化时检测
    public override async Task AfterPowerAmountChanged(PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (power != this) return;
        
        // 压力增加时，提升自己手上（当前玩家）的骑士之剑伤害
        if (amount > 0 && Owner != null && Owner.IsPlayer && CombatState != null)
        {
            int delta = (int)amount;
            if (delta <= 0) return;
            var player = Owner.Player;
            // 只考虑手牌、抽牌堆、弃牌堆、消耗堆
            var piles = new[] { PileType.Hand, PileType.Draw, PileType.Discard,PileType.Exhaust };
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

        await CheckAndTriggerCollapse();
    }

    // 生命值减少时检测
    public override async Task AfterCurrentHpChanged(Creature creature, decimal delta)
    {
        if (creature != Owner) return;
        if (delta >= 0) return; // 只关心生命值减少
        await CheckAndTriggerCollapse();
    }

    private async Task CheckAndTriggerCollapse()
    {
        if (Owner == null || !Owner.IsAlive) return;
        if (Amount >= Owner.CurrentHp)
        {
            // 清除所有压力
            await PowerCmd.ModifyAmount(this, -Amount, Owner, null);
            // 获得一层崩溃
            await PowerCmd.Apply<BreakDownPower>(Owner, 1, Owner, null);
        }
    }

    // 移除自动增加压力的代码，只保留伤害加成
    public override decimal ModifyDamageAdditive(Creature? target, decimal amount, ValueProp props, Creature? dealer,
        CardModel? cardSource)
    {
        if (Owner != target) return 0m;
        return amount * (Amount / 100m);
    }
}