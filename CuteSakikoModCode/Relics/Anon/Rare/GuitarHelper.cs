using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Rare;

public class GuitarHelper : CuteAnonRelic, IChordSequenceModifierProvider
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public IEnumerable<ChordCategory>? AffectedCategories => null;

    public IEnumerable<ChordSequenceModifier> GetModifiers(Creature creature)
    {
        return new[] { new RemoveLastNoteModifier() };
    }
}