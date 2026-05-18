using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
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

    // 回合结束不再移除自身，整场战斗持续
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        await base.AfterTurnEnd(choiceContext, side);
    }

    public override async Task AfterRemoved(Creature oldOwner)
    {
        ClearTemporaryChords();
        await base.AfterRemoved(oldOwner);
    }

    /// <summary> 按当前层数调整临时和弦数量，不改变已有和弦种类 </summary>
    private void AdjustTemporaryChords()
    {
        var owner = Owner;
        if (owner?.Player == null) return;

        var guitar = owner.Player.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) return;

        var targetCount = Amount;
        var existing = guitar.GetTemporaryChords().ToList();
        var currentCount = existing.Count;

        if (currentCount < targetCount)
        {
            // 需要添加新随机和弦（缺少的数量）
            var allPools = new List<string>();
            allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
            allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
            allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));
            if (allPools.Count == 0) return;

            var rng = owner.Player.RunState.Rng.UpFront;
            for (var i = 0; i < targetCount - currentCount; i++)
            {
                var chordId = rng.NextItem(allPools);
                guitar.AddTemporaryChord(chordId);
            }
        }
        else if (currentCount > targetCount)
        {
            // 需要移除多余的临时和弦（从末尾开始移除）
            while (currentCount > targetCount)
            {
                var lastChordId = existing.Last();
                guitar.RemoveTemporaryChord(lastChordId);
                existing.RemoveAt(existing.Count - 1);
                currentCount--;
            }
        }
        // 如果相等，什么都不做
    }

    private void ClearTemporaryChords()
    {
        var owner = Owner;
        if (owner?.Player == null) return;
        var guitar = owner.Player.Relics.OfType<AnonGuitar>().FirstOrDefault();
        guitar?.ClearTemporaryChords();
    }
}