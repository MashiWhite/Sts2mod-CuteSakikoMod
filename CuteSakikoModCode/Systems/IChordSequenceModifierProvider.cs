using MegaCrit.Sts2.Core.Entities.Creatures;

namespace CuteSakikoMod.CuteSakikoModCode.Systems
{
    public interface IChordSequenceModifierProvider
    {
        IEnumerable<ChordSequenceModifier> GetModifiers(Creature owner);
        IEnumerable<ChordCategory>? AffectedCategories { get; } // null 表示所有类别
    }
}