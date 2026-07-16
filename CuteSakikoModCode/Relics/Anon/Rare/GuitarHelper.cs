using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Rare;

public class GuitarHelper : CuteAnonRelic, IChordSequenceModifierProvider
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    public IEnumerable<ChordCategory>? AffectedCategories => null;

    // 更新为新接口，增加 chordDef 参数（此处未使用）
    public IEnumerable<ChordSequenceModifier> GetModifiers(Creature creature, ChordDefinition chordDef)
    {
        return new[] { new RemoveLastNoteModifier() };
    }
}