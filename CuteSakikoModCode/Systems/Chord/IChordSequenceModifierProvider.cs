
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Chord;

public interface IChordSequenceModifierProvider
{
    IEnumerable<ChordCategory>? AffectedCategories { get; }
    // 新增 chordDef 参数，让实现类能知道当前要修改哪个和弦
    IEnumerable<ChordSequenceModifier> GetModifiers(Creature owner, ChordDefinition chordDef);
}