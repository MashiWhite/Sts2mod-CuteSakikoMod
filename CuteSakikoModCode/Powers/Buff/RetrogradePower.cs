using System;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class RetrogradePower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    private int _hpBoostApplied;
    private bool _subscribed;

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
        ApplyMaxHpBoost();   // 初始加成
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        if (_subscribed)
        {
            var manager = FlybackManager.Instance;
            if (manager != null) manager.OnFlybackDataChanged -= OnFlybackDataChanged;
            _subscribed = false;
        }

        // 恢复之前增加的最大生命值
        if (_hpBoostApplied > 0 && oldOwner != null)
        {
            // 注意：使用内部方法或官方API，根据你的框架选择
            oldOwner.SetMaxHpInternal(oldOwner.MaxHp - _hpBoostApplied);
        }
        await base.AfterRemoved(oldOwner);
    }

    private void OnFlybackDataChanged(int playCount, int reloadCount)
    {
        UpdateDynamicInfo(playCount, reloadCount);
        ApplyMaxHpBoost();   // 重新计算并更新最大生命值
    }

    private void UpdateDynamicInfo(int? playCount = null, int? reloadCount = null)
    {
        DynamicVars["FlybackPlayCount"].BaseValue = playCount ?? FlybackManager.Instance?.TotalPlayCount ?? 0;
        DynamicVars["ReloadCount"].BaseValue = reloadCount ?? FlybackManager.GetReloadCount();
    }

    private void ApplyMaxHpBoost()
    {
        int newBoost = CalculateHpBoost();
        if (newBoost == _hpBoostApplied) return;

        // 先移除旧的加成
        if (_hpBoostApplied > 0)
            Owner.SetMaxHpInternal(Owner.MaxHp - _hpBoostApplied);
        // 再应用新的加成
        if (newBoost > 0)
            Owner.SetMaxHpInternal(Owner.MaxHp + newBoost);

        _hpBoostApplied = newBoost;
        DynamicVars["ExtraMaxHp"].BaseValue = newBoost;
    }

    private int CalculateHpBoost()
    {
        int playCount = FlybackManager.Instance?.TotalPlayCount ?? 0;
        int reloads = FlybackManager.GetReloadCount();
        return (int)((playCount / 100f) * reloads);
    }
}