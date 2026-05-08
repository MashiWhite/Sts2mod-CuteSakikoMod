using System;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class TimeWatchPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private int _damageTakenThisTurn;   // 本回合累计的未格挡伤害（用于计算预期恢复）
    private bool _subscribed;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("HealPercent", 0);
            yield return new DynamicVar("HealAmount", 0);        // 预期恢复量（显示用）
            yield return new DynamicVar("FlybackPlayCount", 0);
            yield return new DynamicVar("ReloadCount", 0);
        }
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);

        var manager = FlybackManager.Instance;
        if (manager != null)
        {
            manager.OnFlybackDataChanged -= OnFlybackDataChanged;
            manager.OnFlybackDataChanged += OnFlybackDataChanged;
            _subscribed = true;
        }
        UpdateStaticInfo();
        UpdateHealPercent();      // 设置基础百分比 5%
        UpdateExpectedHeal();     // 初始预期为 0
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_subscribed)
        {
            var manager = FlybackManager.Instance;
            if (manager != null) manager.OnFlybackDataChanged -= OnFlybackDataChanged;
            _subscribed = false;
        }
        await base.AfterRemoved(oldOwner);
    }

    // 记录怪物受到的未格挡伤害，并实时更新预期恢复量
    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner)
        {
            _damageTakenThisTurn += result.UnblockedDamage;
            UpdateExpectedHeal();   // 每次受伤后重新计算预期恢复量
        }
        await Task.CompletedTask;
    }

    // 怪物回合开始时：根据累计伤害恢复生命，然后重置
    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != CombatSide.Enemy || combatState == null) return;

        int healAmount = GetExpectedHealAmount();   // 实际应恢复量
        if (healAmount > 0)
            await CreatureCmd.Heal(Owner, healAmount);

        // 重置伤害累计，并将预期恢复量显示为 0
        _damageTakenThisTurn = 0;
        UpdateExpectedHeal();   // 此时预期恢复量应为 0
    }

    // 飞返计数变化时刷新百分比和预期恢复量
    private void OnFlybackDataChanged(int playCount, int reloadCount)
    {
        DynamicVars["FlybackPlayCount"].BaseValue = playCount;
        DynamicVars["ReloadCount"].BaseValue = reloadCount;
        UpdateHealPercent();      // 动态百分比可能改变
        UpdateExpectedHeal();     // 重新计算预期恢复量
    }

    // 更新静态次数信息（初始化或手动刷新时）
    private void UpdateStaticInfo()
    {
        DynamicVars["FlybackPlayCount"].BaseValue = FlybackManager.Instance?.TotalPlayCount ?? 0;
        DynamicVars["ReloadCount"].BaseValue = FlybackManager.GetReloadCount();
        UpdateHealPercent();
    }

    // 计算当前恢复百分比（基础5% + 额外加成）
    private float GetCurrentHealPercent()
    {
        int playCount = FlybackManager.Instance?.TotalPlayCount ?? 0;
        int reloads = FlybackManager.GetReloadCount();
        float basePercent = 5f;
        float extraPercent = ((float)playCount / 100f) * reloads;
        return basePercent + extraPercent;
    }

    // 根据当前累计伤害和百分比计算预期恢复量
    private int GetExpectedHealAmount()
    {
        float percent = GetCurrentHealPercent();
        return (int)Math.Ceiling(_damageTakenThisTurn * percent / 100f);
    }

    // 更新动态变量中的百分比显示
    private void UpdateHealPercent()
    {
        float percent = GetCurrentHealPercent();
        DynamicVars["HealPercent"].BaseValue = (decimal)Math.Round(percent, 1);
    }

    // 更新动态变量中的预期恢复量显示
    private void UpdateExpectedHeal()
    {
        int expected = GetExpectedHealAmount();
        DynamicVars["HealAmount"].BaseValue = expected;
    }
}