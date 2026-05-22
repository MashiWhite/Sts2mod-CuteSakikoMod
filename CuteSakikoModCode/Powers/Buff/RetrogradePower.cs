using CuteSakikoMod.CuteSakikoModCode.Monsters.Boss;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class RetrogradePower : CuteSakikoModPower
{
    private int _hpBoostApplied;
    private bool _subscribed;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("ExtraMaxHp", 0);
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

        UpdateDynamicInfo();
        await ApplyMaxHpBoost();
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_subscribed)
        {
            var manager = FlybackManager.Instance;
            if (manager != null) manager.OnFlybackDataChanged -= OnFlybackDataChanged;
            _subscribed = false;
        }

        if (_hpBoostApplied > 0 && oldOwner != null)
            oldOwner.SetMaxHpInternal(oldOwner.MaxHp - _hpBoostApplied);
        await base.AfterRemoved(oldOwner);
    }

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState )
    {
        if (side == CombatSide.Enemy)
        {
            await ApplyMaxHpBoost();
        }
    }

    private void OnFlybackDataChanged(int playCount, int reloadCount)
    {
        UpdateDynamicInfo(playCount, reloadCount);
        _ = ApplyMaxHpBoost();
    }

    private void UpdateDynamicInfo(int? playCount = null, int? reloadCount = null)
    {
        DynamicVars["FlybackPlayCount"].BaseValue = playCount ?? FlybackManager.Instance?.TotalPlayCount ?? 0;
        DynamicVars["ReloadCount"].BaseValue = reloadCount ?? FlybackManager.GetReloadCount();
    }

    private async Task ApplyMaxHpBoost()
    {
        var newBoost = CalculateHpBoost();
        var oldBoost = _hpBoostApplied;
        if (newBoost == oldBoost) return;
        if (oldBoost > 0) Owner.SetMaxHpInternal(Owner.MaxHp - oldBoost);
        if (newBoost > 0) Owner.SetMaxHpInternal(Owner.MaxHp + newBoost);

        var increase = newBoost - oldBoost;
        if (increase > 0 && Owner != null) await CreatureCmd.Heal(Owner, increase);
        _hpBoostApplied = newBoost;
        DynamicVars["ExtraMaxHp"].BaseValue = newBoost;
    }

    private int CalculateHpBoost()
    {
        var playCount = FlybackManager.Instance?.TotalPlayCount ?? 0;
        var reloads = FlybackManager.GetReloadCount();
        return (int)(playCount / 100f * reloads);
    }

    public async Task RefreshHpBoost()
    {
        await ApplyMaxHpBoost();
    }

    // ========== 复活逻辑 ==========
    public override bool ShouldPowerBeRemovedAfterOwnerDeath()
    {
        return false;
    }

    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
    {
        return creature != Owner;
    }

    public override bool ShouldStopCombatFromEnding()
    {
        return true;
    }

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (wasRemovalPrevented || creature != Owner) return;

        if (creature.Monster is StarAnon starAnon)
            await starAnon.TriggerDeadState();
    }
}