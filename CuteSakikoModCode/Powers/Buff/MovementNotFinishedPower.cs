using CuteSakikoMod.CuteSakikoModCode.Monsters.Boss.TenDay;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class MovementNotFinishedPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    /// <summary>
    /// SmartDescription 本身不可重写；但它的 key 来自 protected virtual SmartDescriptionLocKey。
    /// 按十日终焉层数切到 effect.N 词条。
    /// </summary>
    protected override string SmartDescriptionLocKey
    {
        get
        {
            // Canonical（图鉴/预览）时不能碰 Owner，必须短路
            if (!IsMutable)
                return base.SmartDescriptionLocKey;

            var day = Owner.GetPower<TenDayEndPower>()?.Amount ?? 10;
            var key = $"{Id.Entry}.effect.{day}";

            return LocString.Exists("powers", key) ? key : base.SmartDescriptionLocKey;
        }
    }

    private readonly HashSet<int> _triggeredDays = new();
    private bool _deathShieldAvailable;
    private bool _deathShieldUsed;

    // ───────── 伤害减免：每只存活小怪 5% ─────────
    private decimal GetDamageReduction()
    {
        var combatState = Owner.CombatState;
        if (combatState == null) return 0m;

        var aliveMinions = combatState.Enemies
            .Count(e => e != Owner && e.IsAlive && e.HasPower<MinionPower>());

        return 0.05m * aliveMinions;
    }

    public override Decimal ModifyHpLostBeforeOstyLate(
        Creature target,
        Decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner || amount <= 0) return amount;
        var reduction = GetDamageReduction();
        return amount * (1m - reduction);
    }

    // ───────── 一次性死亡防护 ─────────
    public override bool ShouldDie(Creature creature)
    {
        if (creature != Owner) return base.ShouldDie(creature);
        if (!_deathShieldAvailable || _deathShieldUsed) return base.ShouldDie(creature);
        return false;
    }

    public override async Task AfterPreventingDeath(Creature creature)
    {
        if (creature != Owner) return;
        if (!_deathShieldAvailable || _deathShieldUsed) return;

        _deathShieldUsed = true;
        var heal = Math.Max(1, (int)(creature.MaxHp * 0.5m));
        await CreatureCmd.Heal(creature, heal);
    }

    // ───────── 日数变化时触发 ─────────
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is not TenDayEndPower endPower) return;
        if (endPower.Owner != Owner) return;

        Owner.GetPower<BeatingHeartPower>()?.UpdateCap();

        int day = endPower.Amount;
        if (!_triggeredDays.Add(day)) return;

        await TriggerDayEffect(choiceContext, day);
    }

    private async Task TriggerDayEffect(PlayerChoiceContext choiceContext, int day)
    {
        if (Owner.Monster is not TenDayTaki boss) return;

        switch (day)
        {
            case 10:
                break;

            case 9:
                await PowerCmd.Apply<BeatingHeartPower>(
                    new ThrowingPlayerChoiceContext(), Owner, 1, Owner, null);
                Owner.GetPower<BeatingHeartPower>()?.UpdateCap();
                break;

            case 8:
                _deathShieldAvailable = true;
                break;

            case 7:
                await boss.SummonAlly<TenDayAnon>(choiceContext, "snail1");
                await CreatureCmd.Heal(Owner, (int)(Owner.MaxHp * 0.10m));
                break;

            case 6:
                await boss.SummonAlly<TenDayRana>(choiceContext, "snail2");
                break;

            case 5:
                await boss.SummonAlly<TenDayTomori>(choiceContext, "snail3");
                break;

            case 4:
                await boss.SummonAlly<TenDayUmirin>(choiceContext, "snail4");
                break;

            case 3:
                await boss.SummonAlly<TenDaySoyo>(choiceContext, "snail5");
                break;

            case 2:
                await boss.SummonAlly<TenDaySaki>(choiceContext, "snail6");
                break;

            case 1:
                foreach (var player in Owner.CombatState.Players)
                {
                    await CreatureCmd.LoseMaxHp(
                        new ThrowingPlayerChoiceContext(), player.Creature, 10m, false);
                }
                break;
        }
    }
}