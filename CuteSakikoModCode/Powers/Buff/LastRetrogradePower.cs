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
using System.Reflection;
using MegaCrit.Sts2.Core.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class LastRetrogradePower : CuteSakikoModPower,
    IPowerExtraIconAmountLabelsProvider,
    IPowerExtraIconAmountLabelsChangeSource
{
    private int _hpBoostApplied;
    private bool _subscribed;
    private int _totalCardWeight;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("ExtraMaxHp", 0);
            yield return new DynamicVar("FlybackPlayCount", 0);
            yield return new DynamicVar("ReloadCount", 0);
            yield return new DynamicVar("Countdown", 0);
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
                Text = $"{_totalCardWeight}/{GetTotalLimit()}"
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

        var countdown = Math.Max(0, (int)DynamicVars["Countdown"].BaseValue - weight);
        DynamicVars["Countdown"].BaseValue = countdown;
        InvalidateLabels();

        if (_totalCardWeight >= GetTotalLimit())
        {
            _totalCardWeight = 0;
            DynamicVars["Countdown"].BaseValue = GetTotalLimit();
            InvalidateLabels();

            // ★ 关键修复：等待所有玩家选择完成，避免与 DoAnything 等选择界面冲突
            await WaitForAllChoices();

            if (Owner?.CombatState != null)
                foreach (var p in Owner.CombatState.Players)
                    PlayerCmd.EndTurn(p, false);
        }
    }

    // ★ 等待所有进行中的玩家选择完成
    private async Task WaitForAllChoices()
    {
        var sync = RunManager.Instance?.PlayerChoiceSynchronizer;
        if (sync == null) return;

        // 通过反射获取私有字段 _pendingChoices，判断是否还有等待中的选择
        var field = sync.GetType().GetField("_pendingChoices", BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) return;

        while (true)
        {
            var pending = field.GetValue(sync) as System.Collections.IEnumerable;
            if (pending == null) break;

            // 如果集合中没有元素，则退出循环
            var hasAny = false;
            foreach (var _ in pending) { hasAny = true; break; }
            if (!hasAny) break;

            await Cmd.Wait(0.1f);
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
        return (int)(playCount * (float)reloads / 10);
    }

    public async Task RefreshHpBoost()
    {
        await ApplyMaxHpBoost();
    }
}