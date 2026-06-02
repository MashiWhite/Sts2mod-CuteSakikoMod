using CuteSakikoMod.CuteSakikoModCode.Monsters.Boss;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;

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

    public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side == CombatSide.Enemy) await ApplyMaxHpBoost();
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
        return (int)(playCount * (float)reloads / 5);  // 整数除法，向下取整
    }

    public async Task RefreshHpBoost()
    {
        await ApplyMaxHpBoost();
    }

    // ========== 复活逻辑（修改后） ==========
    public override bool ShouldPowerBeRemovedAfterOwnerDeath() => false;

    public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature) => creature != Owner;

    public override bool ShouldStopCombatFromEnding() => true;

    public override async Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        if (wasRemovalPrevented || creature != Owner) return;

        // ★ 关键修复：确保视觉节点存在（如果已被销毁则重新创建）
        var combatRoom = NCombatRoom.Instance;
        if (combatRoom != null)
        {
            var existingNode = combatRoom.GetCreatureNode(Owner);
            if (existingNode == null)
            {
                // 节点已被删除，重新添加（会自动创建 NCreature 并放入正确容器）
                combatRoom.AddCreature(Owner);
                // 等待一帧让节点初始化完成（可选，确保后续动画正常）
                await Task.Delay(50);
            }
        }

        // 触发复活（此时视觉节点已保证存在）
        if (creature.Monster is StarAnon starAnon)
            await starAnon.TriggerDeadState();
    }
}