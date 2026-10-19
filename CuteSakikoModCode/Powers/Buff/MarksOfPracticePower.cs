using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public class MarksOfPracticePower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    [SavedProperty]
    private string MyChordIdsData { get; set; } = "";

    private List<string> GetMyChords()
        => string.IsNullOrEmpty(MyChordIdsData)
            ? new List<string>()
            : MyChordIdsData.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

    private void SetMyChords(List<string> chords)
        => MyChordIdsData = string.Join(";", chords);

    public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        await base.AfterApplied(applier, cardSource);
        AdjustTemporaryChords();
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
        if (power == this)
            AdjustTemporaryChords();
    }

    public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        await base.AfterSideTurnEnd(choiceContext, side, participants);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        ClearTemporaryChords();
        await base.AfterRemoved(oldOwner);
    }

    private void AdjustTemporaryChords()
    {
        var owner = Owner;
        if (owner?.Player == null) return;

        var chords = owner.Player.GetChords();
        if (chords == null) return;

        var targetCount = Amount;
        var myChords = GetMyChords();

        // 1. 移除多余的（从末尾删，即最近加的）
        while (myChords.Count > targetCount)
        {
            var toRemove = myChords[^1];
            chords.RemoveTemporaryChord(toRemove);
            myChords.RemoveAt(myChords.Count - 1);
        }

        // 2. 添加不足的
        if (myChords.Count < targetCount)
        {
            var needed = targetCount - myChords.Count;
            var added = ChordCmd.AddRandomTemporaryChords(chords, needed);
            myChords.AddRange(added);   // ★ 追踪新增的（它们也已被 LearnChord 学习）
        }

        SetMyChords(myChords);
    }

    private void ClearTemporaryChords()
    {
        var owner = Owner;
        if (owner?.Player == null) return;

        var chords = owner.Player.GetChords();
        if (chords == null) return;

        foreach (var chordId in GetMyChords())
            chords.RemoveTemporaryChord(chordId);

        SetMyChords(new List<string>());
    }
}