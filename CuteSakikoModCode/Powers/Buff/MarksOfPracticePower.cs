
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public class MarksOfPracticePower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

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

        // 只统计 / 只回收「非 TemporaryOnly」的临时和弦。
        // TemporaryOnly 的那批（AnonCChord / AnonDChord / AnonEChord / AnonFChord /
        // AnonGChord / HekitenbansouChord）是别的卡牌用来「注册待演奏」的载体
        // （见 LookCchord / Hekitenbansou 的 OnPlay：先 AddTemporaryChord，再打出才 PlayChordAsync）。
        // 它们不归本能力管 —— 按整表数量裁剪会把它们误删，导致那些卡的效果永远发不出来。
        var mine = chords.GetTemporaryChords()
            .Where(id => !ChordManager.AllChords.TryGetValue(id, out var def) || !def.IsTemporaryOnly)
            .ToList();

        var targetCount = Amount;
        if (mine.Count < targetCount)
        {
            // AddRandomTemporaryChords 的第二个参数语义是「目标总条数」（内部按整表差值计算），
            // 因此传入「当前整表条数 + 本能力还需要补的量」。
            var need = targetCount - mine.Count;
            ChordCmd.AddRandomTemporaryChords(chords, chords.GetTemporaryChords().Count + need);
        }
        else if (mine.Count > targetCount)
        {
            while (mine.Count > targetCount)
            {
                var lastChordId = mine[^1];
                mine.RemoveAt(mine.Count - 1);
                chords.RemoveTemporaryChord(lastChordId);
            }
        }
    }

    private void ClearTemporaryChords()
    {
        var owner = Owner;
        if (owner?.Player == null) return;

        var chords = owner.Player.GetChords();
        if (chords == null) return;

        // 同样只回收本能力加的那些，不要整表 Clear ——
        // 否则能力消失时会连带清掉别的卡注册的临时和弦。
        foreach (var id in chords.GetTemporaryChords().ToList())
            if (!ChordManager.AllChords.TryGetValue(id, out var def) || !def.IsTemporaryOnly)
                chords.RemoveTemporaryChord(id);
    }
}