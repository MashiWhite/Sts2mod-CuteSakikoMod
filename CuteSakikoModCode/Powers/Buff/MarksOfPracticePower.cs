using System.Collections.Generic;
using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
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

    /// <summary> 按当前层数调整临时和弦数量，不改变已有和弦种类，且避免重复 </summary>
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
            // 收集所有已拥有的和弦ID（分类、奖励、临时）
            var ownedChordIds = new HashSet<string>();
            foreach (var kv in guitar.GetCurrentChords())
                if (!string.IsNullOrEmpty(kv.Value))
                    ownedChordIds.Add(kv.Value);
            foreach (var id in guitar.GetBonusChords())
                ownedChordIds.Add(id);
            foreach (var id in existing)
                ownedChordIds.Add(id); // 已存在的临时和弦

            // 构建候选池，排除已拥有
            var allPools = new List<string>();
            allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
            allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
            allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));

            var available = allPools.Where(id => !ownedChordIds.Contains(id)).ToList();
            if (available.Count == 0)
                return; // 没有可用的新和弦

            var rng = owner.Player.RunState.Rng.UpFront;
            for (var i = 0; i < targetCount - currentCount; i++)
            {
                // 每次抽取前重新检查可用池（避免多次抽取重复）
                if (available.Count == 0)
                    break; // 已无可用，停止添加

                var chordId = rng.NextItem(available);
                guitar.AddTemporaryChord(chordId);

                // 从可用池中移除已添加的，避免下次再抽到相同的
                available.Remove(chordId);
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
    }

    private void ClearTemporaryChords()
    {
        var owner = Owner;
        if (owner?.Player == null) return;
        var guitar = owner.Player.Relics.OfType<AnonGuitar>().FirstOrDefault();
        guitar?.ClearTemporaryChords();
    }
}