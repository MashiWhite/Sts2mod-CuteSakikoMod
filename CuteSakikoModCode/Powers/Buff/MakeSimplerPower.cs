
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public class MakeSimplerPower : CuteSakikoModPower, IChordSequenceModifierProvider
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public IEnumerable<ChordCategory>? AffectedCategories => null;

    // 缓存修饰符结果，键为和弦ID
    private Dictionary<string, List<ChordSequenceModifier>> _cachedModifiers = new();

    public IEnumerable<ChordSequenceModifier> GetModifiers(Creature owner, ChordDefinition chordDef)
    {
        if (Amount <= 0) yield break;

        // 有缓存直接返回
        if (_cachedModifiers.TryGetValue(chordDef.Id, out var cached))
        {
            foreach (var mod in cached)
                yield return mod;
            yield break;
        }

        var guitar = owner.Player?.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) yield break;

        var allChordIds = guitar.GetEquippedChordIds();
        if (!allChordIds.Contains(chordDef.Id)) yield break;

        int noteCount = chordDef.NoteSequence.Length;
        if (noteCount == 0) yield break;

        int replaceCount = Math.Min(Amount, noteCount);

        var combatState = owner.CombatState;
        if (combatState == null) yield break;

        var rng = combatState.RunState.Rng.Niche;

        var indices = Enumerable.Range(0, noteCount).ToList();
        for (int i = indices.Count - 1; i > 0; i--)
        {
            int j = rng.NextInt(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        var newModifiers = new List<ChordSequenceModifier>();
        for (int i = 0; i < replaceCount; i++)
        {
            var mod = new ReplaceNoteModifier(indices[i], Entry.AnyNote);
            newModifiers.Add(mod);
            yield return mod;
        }

        // 存入缓存
        _cachedModifiers[chordDef.Id] = newModifiers;
    }

    // 当层数改变时清除缓存，保证效果更新
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);
        if (power == this)
            _cachedModifiers.Clear();
    }
}