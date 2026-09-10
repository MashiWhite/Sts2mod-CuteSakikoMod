using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Common;

public class Recorder : CuteAnonRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (side != Owner.Creature.Side || combatState.RoundNumber != 1)
            return;

        if (Owner.Relics.OfType<AnonGuitar>().FirstOrDefault() == null) return;

        await ChordNoteSystem.PlayRandomEquippedChordImmediateAsync(Owner);
        Flash();
    }
}