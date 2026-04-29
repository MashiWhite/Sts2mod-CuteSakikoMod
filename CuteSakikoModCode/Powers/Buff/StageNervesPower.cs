using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class StageNervesPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    /// <summary>
    ///     音符未构成和弦时触发，获得等于层数的格挡。
    /// </summary>
    public async Task OnNoteWithoutChord()
    {
        if (Amount > 0) await CreatureCmd.GainBlock(Owner, Amount, 0, null);
    }
}