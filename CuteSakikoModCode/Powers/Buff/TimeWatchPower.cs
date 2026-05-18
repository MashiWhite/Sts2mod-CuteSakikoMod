using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class TimeWatchPower : CuteSakikoModPower
{
    private int _damageTakenThisTurn;
    private bool _subscribed;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("HealPercent", 0); // 仅额外百分比
            yield return new DynamicVar("HealAmount", 0); // 预期恢复量
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
        UpdateHealPercent();
        UpdateExpectedHeal();
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

    public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target,
        DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (target == Owner)
        {
            _damageTakenThisTurn += result.UnblockedDamage;
            UpdateExpectedHeal();
        }

        await Task.CompletedTask;
    }

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side != CombatSide.Enemy || combatState == null) return;

        var healAmount = GetExpectedHealAmount();
        if (healAmount > 0)
            await CreatureCmd.Heal(Owner, healAmount);

        _damageTakenThisTurn = 0;
        UpdateExpectedHeal();
    }

    private void OnFlybackDataChanged(int playCount, int reloadCount)
    {
        DynamicVars["FlybackPlayCount"].BaseValue = playCount;
        DynamicVars["ReloadCount"].BaseValue = reloadCount;
        UpdateHealPercent();
        UpdateExpectedHeal();
    }

    private void UpdateStaticInfo()
    {
        DynamicVars["FlybackPlayCount"].BaseValue = FlybackManager.Instance?.TotalPlayCount ?? 0;
        DynamicVars["ReloadCount"].BaseValue = FlybackManager.GetReloadCount();
        UpdateHealPercent();
    }

    // 仅额外百分比： (飞返打出次数% × 读档次数)
    private float GetExtraHealPercent()
    {
        var playCount = FlybackManager.Instance?.TotalPlayCount ?? 0;
        var reloads = FlybackManager.GetReloadCount();
        return playCount / 100f * reloads;
    }

    // 总治疗百分比 = 基础10% + 额外百分比
    private float GetTotalHealPercent()
    {
        return 10f + GetExtraHealPercent();
    }

    // 实际预期恢复量（使用总百分比）
    private int GetExpectedHealAmount()
    {
        var totalPercent = GetTotalHealPercent();
        return (int)Math.Ceiling(_damageTakenThisTurn * totalPercent / 100f);
    }

    // 更新 HealPercent 动态变量（只显示额外部分）
    private void UpdateHealPercent()
    {
        var extra = GetExtraHealPercent();
        DynamicVars["HealPercent"].BaseValue = (decimal)Math.Round(extra, 1);
    }

    private void UpdateExpectedHeal()
    {
        var expected = GetExpectedHealAmount();
        DynamicVars["HealAmount"].BaseValue = expected;
    }
}