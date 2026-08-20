using CuteSakikoMod.CuteSakikoModCode.Systems;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public class FullAssaultPower : CuteSakikoModPower, IChordSequenceModifierProvider
{
    private bool _upgraded;

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    // FullAssaultPower.cs
    public IEnumerable<ChordSequenceModifier> GetModifiers(Creature owner, ChordDefinition chordDef)
    {
        if (_upgraded)
        {
            for (int i = 0; i < 4; i++)
                yield return new ReplaceNoteModifier(i, CardType.Attack);
        }
        else
        {
            yield return new ReplaceNoteModifier(0, CardType.Attack);
        }
    }

    public IEnumerable<ChordCategory>? AffectedCategories => null; // 影响所有类别

    public void SetUpgraded(bool upgraded)
    {
        _upgraded = upgraded;
    }
}