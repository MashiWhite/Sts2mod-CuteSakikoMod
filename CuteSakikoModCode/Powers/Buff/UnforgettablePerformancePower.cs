using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class UnforgettablePerformancePower : CuteSakikoModPower,
    IPowerExtraIconAmountLabelsProvider,
    IPowerExtraIconAmountLabelsChangeSource
{
    private int _playedCount;
    private int _threshold = 3; // 默认 3，施加时会被覆盖
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("Count", _playedCount);
            yield return new DynamicVar("Threshold", _threshold);
        }
    }

    public event Action? PowerExtraIconAmountLabelsInvalidated;

    // IPowerExtraIconAmountLabelsProvider 实现：左下角显示进度
    public IReadOnlyList<ExtraIconAmountLabelSlot> GetPowerExtraIconAmountLabelSlots()
    {
        return new[]
        {
            new ExtraIconAmountLabelSlot
            {
                Corner = ExtraIconAmountLabelCorner.BottomLeft,
                Text = $"{_playedCount}/{_threshold}"
            }
        };
    }

    /// <summary>
    ///     更新阈值（由卡牌打出时调用）
    /// </summary>
    public void UpdateThreshold(int threshold)
    {
        _threshold = threshold;
        DynamicVars["Threshold"]!.BaseValue = _threshold;

        // 如果当前计数超过新阈值，重置计数
        if (_playedCount >= _threshold)
        {
            _playedCount = 0;
            DynamicVars["Count"]!.BaseValue = _playedCount;
        }

        InvalidateLabels();
    }

    /// <summary>
    ///     每次和弦演奏时调用，返回 true 表示触发了能量获得
    /// </summary>
    public bool OnChordPlayed()
    {
        _playedCount++;
        DynamicVars["Count"]!.BaseValue = _playedCount;
        InvalidateLabels();

        if (_playedCount >= _threshold)
        {
            _playedCount = 0;
            DynamicVars["Count"]!.BaseValue = _playedCount;
            InvalidateLabels();
            return true;
        }

        return false;
    }

    private void InvalidateLabels()
    {
        PowerExtraIconAmountLabelsInvalidated?.Invoke();
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        // 根据施加时卡牌升级状态设置初始阈值
        UpdateThreshold(cardSource is UnforgettablePerformance { IsUpgraded: true } ? 2 : 3);
    }
}