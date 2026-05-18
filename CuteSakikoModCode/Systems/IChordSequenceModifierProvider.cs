using MegaCrit.Sts2.Core.Entities.Creatures;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public interface IChordSequenceModifierProvider
{
    IEnumerable<ChordCategory>? AffectedCategories { get; } // null 表示所有类别
    IEnumerable<ChordSequenceModifier> GetModifiers(Creature owner);
}