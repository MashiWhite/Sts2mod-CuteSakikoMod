using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

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

        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) return;

        var chordIds = guitar.GetLearnedChordIds();
        if (chordIds.Count == 0) return;

        // 随机选择 1 个和弦并演奏
        var randomChord = Owner.RunState.Rng.CombatCardSelection.NextItem(chordIds);
        if (ChordManager.AllChords.TryGetValue(randomChord, out var def))
            await def.Effect(new ThrowingPlayerChoiceContext(), Owner.Creature, guitar.GetEffectMultiplier());

        Flash();
    }
}