
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff
{

public class MarksOfPracticePower : CuteSakikoModPower
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter; // 可叠加

        public override async Task AfterApplied(Creature? applier, CardModel? cardSource)
        {
            await base.AfterApplied(applier, cardSource);
            RefreshTemporaryChords();
        }

        public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
        {
            if (side == CombatSide.Player)
            {
                ClearTemporaryChords();
                // 移除所有层数
                RemoveInternal();
            }
            await base.AfterTurnEnd(choiceContext, side);
        }

        public override async Task AfterRemoved(Creature oldOwner)
        {
            ClearTemporaryChords();
            await base.AfterRemoved(oldOwner);
        }

        private void RefreshTemporaryChords()
        {
            var owner = this.Owner;
            if (owner?.Player == null) return;

            var guitar = owner.Player.Relics.OfType<AnonGuitar>().FirstOrDefault();
            if (guitar == null) return;

            // 先清空旧的临时和弦，再根据当前 Amount 生成新的
            guitar.ClearTemporaryChords();

            int count = this.Amount;
            if (count <= 0) return;

            var allPools = new List<string>();
            allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
            allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
            allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));
            if (allPools.Count == 0) return;

            var rng = owner.Player.RunState.Rng.UpFront;
            for (int i = 0; i < count; i++)
                guitar.AddTemporaryChord(rng.NextItem(allPools));
        }

        private void ClearTemporaryChords()
        {
            var owner = this.Owner;
            if (owner?.Player == null) return;
            var guitar = owner.Player.Relics.OfType<AnonGuitar>().FirstOrDefault();
            guitar?.ClearTemporaryChords();
        }
    }
}