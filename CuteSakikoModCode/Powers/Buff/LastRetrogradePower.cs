using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mod.Token;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class LastRetrogradePower : CuteSakikoModPower,
    IPowerExtraIconAmountLabelsProvider,
    IPowerExtraIconAmountLabelsChangeSource
{
    private int _hpBoostApplied;
    private bool _subscribed;
    private int _totalFlybackCount;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public event Action? PowerExtraIconAmountLabelsInvalidated;

    public IReadOnlyList<ExtraIconAmountLabelSlot> GetPowerExtraIconAmountLabelSlots()
    {
        return new[]
        {
            new ExtraIconAmountLabelSlot
            {
                Corner = ExtraIconAmountLabelCorner.BottomLeft,
                Text = $"{_totalFlybackCount}/8"
            }
        };
    }

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("ExtraMaxHp", 0);
            yield return new DynamicVar("FlybackPlayCount", 0);
            yield return new DynamicVar("ReloadCount", 0);
            yield return new DynamicVar("Countdown", 8);
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

    public override async Task AfterSideTurnStart(CombatSide side, ICombatState combatState)
    {
        if (side == CombatSide.Player)
        {
            _totalFlybackCount = 0;
            UpdateCountdownDisplay();
            InvalidateLabels();
        }
        else if (side == CombatSide.Enemy)
        {
            // 敌人回合开始时，数据已在招式内同步，直接刷新
            await ApplyMaxHpBoost();
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card is not Flyback) return;
        var player = cardPlay.Card.Owner;
        if (player == null) return;

        _totalFlybackCount++;
        UpdateCountdownDisplay();
        InvalidateLabels();

        if (_totalFlybackCount >= 8)
        {
            _totalFlybackCount = 0;
            UpdateCountdownDisplay();
            InvalidateLabels();

            if (Owner?.CombatState != null)
            {
                foreach (var p in Owner.CombatState.Players)
                    PlayerCmd.EndTurn(p, false);
            }
        }
    }

    private void UpdateCountdownDisplay()
    {
        int remaining = Math.Max(8 - _totalFlybackCount, 0);
        DynamicVars["Countdown"].BaseValue = remaining;
    }

    private void InvalidateLabels()
    {
        PowerExtraIconAmountLabelsInvalidated?.Invoke();
        InvokeDisplayAmountChanged();
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
}