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
    private int _totalCardWeight; // 当前回合累计打出的卡牌权重（用于UI显示）

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("ExtraMaxHp", 0);
            yield return new DynamicVar("FlybackPlayCount", 0);
            yield return new DynamicVar("ReloadCount", 0);
            yield return new DynamicVar("Countdown", 0); // ★ 加回 Countdown 变量
        }
    }

    public event Action? PowerExtraIconAmountLabelsInvalidated;

    public IReadOnlyList<ExtraIconAmountLabelSlot> GetPowerExtraIconAmountLabelSlots()
    {
        return new[]
        {
            new ExtraIconAmountLabelSlot
            {
                Corner = ExtraIconAmountLabelCorner.BottomLeft,
                Text = $"{_totalCardWeight}/{GetTotalLimit()}" // 显示 当前权重/上限
            }
        };
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
        if (side == CombatSide.Player)
        {
            _totalCardWeight = 0;
            // ★ 初始化 Countdown 为当前上限
            DynamicVars["Countdown"].BaseValue = GetTotalLimit();
            InvalidateLabels();
        }
        else if (side == CombatSide.Enemy)
        {
            await ApplyMaxHpBoost();
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var weight = cardPlay.Card is Flyback ? 5 : 1;
        _totalCardWeight += weight;

        // ★ 更新 Countdown（剩余可打出权重）
        var countdown = Math.Max(0, (int)DynamicVars["Countdown"].BaseValue - weight);
        DynamicVars["Countdown"].BaseValue = countdown;
        InvalidateLabels();

        if (_totalCardWeight >= GetTotalLimit())
        {
            _totalCardWeight = 0;
            DynamicVars["Countdown"].BaseValue = GetTotalLimit(); // 重置
            InvalidateLabels();

            if (Owner?.CombatState != null)
                foreach (var p in Owner.CombatState.Players)
                    PlayerCmd.EndTurn(p, false);
        }
    }

    private int GetTotalLimit()
    {
        var reloads = FlybackManager.GetReloadCount();
        var basePerPlayer = Math.Max(5, 44 - reloads * 3);
        var playerCount = Owner?.CombatState?.Players.Count ?? 1;
        return basePerPlayer * playerCount;
    }

    private void InvalidateLabels()
    {
        PowerExtraIconAmountLabelsInvalidated?.Invoke();
        InvokeDisplayAmountChanged();
    }

    // 以下方法保持不变
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
}