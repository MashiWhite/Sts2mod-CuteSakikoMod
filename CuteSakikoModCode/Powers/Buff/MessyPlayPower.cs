using System;
using System.Collections.Generic;
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class MessyPlayPower : CuteSakikoModPower,
    IPowerExtraIconAmountLabelsProvider,
    IPowerExtraIconAmountLabelsChangeSource
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 层数 = 额外音符数

    private int _noteCount;
    private int _threshold;
    public bool IsGeneratingNotes { get; private set; }

    public void StartGeneratingNotes() => IsGeneratingNotes = true;
    public void EndGeneratingNotes() => IsGeneratingNotes = false;

    // 公开方法，用于升级或重新施加时更新阈值
    public void UpdateThreshold(int newThreshold)
    {
        _threshold = Math.Min(_threshold == 0 ? newThreshold : _threshold, newThreshold);
        if (_noteCount > _threshold)
            _noteCount = _threshold;   // 进度不能超过阈值
        InvalidateLabels();
        InvokeDisplayAmountChanged();  // 刷新能力图标（确保角标更新）
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        int newThreshold = (cardSource is MessyPlay { IsUpgraded: true }) ? 2 : 3;
        UpdateThreshold(newThreshold); // 首次施加时也会设置阈值
        IsGeneratingNotes = false;
    }

    public bool OnNoteObtained()
    {
        if (IsGeneratingNotes) return false;
        _noteCount++;
        InvalidateLabels();
        return _noteCount >= _threshold;
    }

    public void ResetNoteCount()
    {
        _noteCount = 0;
        InvalidateLabels();
    }

    private void InvalidateLabels()
    {
        PowerExtraIconAmountLabelsInvalidated?.Invoke();
        InvokeDisplayAmountChanged();  // 双保险
    }

    public event Action? PowerExtraIconAmountLabelsInvalidated;

    public IReadOnlyList<ExtraIconAmountLabelSlot> GetPowerExtraIconAmountLabelSlots()
    {
        return new[]
        {
            new ExtraIconAmountLabelSlot
            {
                Corner = ExtraIconAmountLabelCorner.BottomLeft,
                Text = $"{_noteCount}/{_threshold}"
            }
        };
    }
}