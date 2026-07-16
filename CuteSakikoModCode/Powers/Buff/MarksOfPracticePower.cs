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

        var guitar = owner.Player.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) return;

        var targetCount = Amount;
        var existing = guitar.GetTemporaryChords().ToList();
        var currentCount = existing.Count;

        if (currentCount < targetCount)
        {
            // 调用封装方法添加随机临时和弦
            ChordCmd.AddRandomTemporaryChords(guitar, targetCount);
        }
        else if (currentCount > targetCount)
        {
            // 移除多余的临时和弦
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