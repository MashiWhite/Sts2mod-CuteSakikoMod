using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Combat.Ui.ExtraCornerAmountLabels;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class MessyPlayPower : CuteSakikoModPower,
    IPowerExtraIconAmountLabelsProvider,
    IPowerExtraIconAmountLabelsChangeSource
{
    private int _noteCount;
    private int _threshold;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public bool IsGeneratingNotes { get; private set; }

    // 暴露给描述系统
    private DynamicVar NoteCountVar => DynamicVars["NoteCount"];
    private DynamicVar ThresholdVar => DynamicVars["Threshold"];

    protected override IEnumerable<DynamicVar> CanonicalVars
    {
        get
        {
            yield return new DynamicVar("NoteCount", 0);
            yield return new DynamicVar("Threshold", 0);
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
                Text = $"{_noteCount}/{_threshold}"
            }
        };
    }

    public void StartGeneratingNotes()
    {
        IsGeneratingNotes = true;
    }

    public void EndGeneratingNotes()
    {
        IsGeneratingNotes = false;
    }

    public void UpdateThreshold(int newThreshold)
    {
        _threshold = Math.Min(_threshold == 0 ? newThreshold : _threshold, newThreshold);
        if (_noteCount > _threshold)
            _noteCount = _threshold;
        ThresholdVar.BaseValue = _threshold;
        NoteCountVar.BaseValue = _noteCount;
        InvalidateLabels();
        InvokeDisplayAmountChanged();
    }

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        var newThreshold = cardSource is MessyPlay { IsUpgraded: true } ? 2 : 3;
        UpdateThreshold(newThreshold);
        IsGeneratingNotes = false;
    }

    public bool OnNoteObtained()
    {
        if (IsGeneratingNotes) return false;
        _noteCount++;
        NoteCountVar.BaseValue = _noteCount;
        InvalidateLabels();
        return _noteCount >= _threshold;
    }

    public void ResetNoteCount()
    {
        _noteCount = 0;
        NoteCountVar.BaseValue = 0;
        InvalidateLabels();
    }

    private void InvalidateLabels()
    {
        PowerExtraIconAmountLabelsInvalidated?.Invoke();
        InvokeDisplayAmountChanged();
    }
}