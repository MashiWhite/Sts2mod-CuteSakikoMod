using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace CuteSakikoMod.CuteSakikoModCode.Systems
{
    public static class ChordSequenceModifierHelper
    {
        // 收集生物身上所有活跃的修改器
        public static List<ChordSequenceModifier> CollectModifiers(Creature creature, ChordDefinition chordDef)
        {
            var result = new List<ChordSequenceModifier>();
            if (creature == null) return result;

            foreach (var provider in creature.Powers.OfType<IChordSequenceModifierProvider>())
            {
                var cats = provider.AffectedCategories;
                if (cats == null || !cats.Any() || cats.Contains(chordDef.Category))
                {
                    result.AddRange(provider.GetModifiers(creature));
                }
            }
            return result;
        }

        // 依次应用所有修改器，获得修改后的音符序列
        public static IReadOnlyList<CardType> GetModifiedSequence(ChordDefinition chordDef, Creature owner)
        {
            var mods = CollectModifiers(owner, chordDef);
            IReadOnlyList<CardType> seq = chordDef.NoteSequence;
            foreach (var mod in mods)
                seq = mod.Apply(seq);
            return seq;
        }

        // 生成修改后的条件文本（用于UI）
        public static string GetModifiedConditionText(ChordDefinition chordDef, Creature owner)
        {
            var seq = GetModifiedSequence(chordDef, owner);
            var parts = new List<string>();
            foreach (var t in seq)
            {
                parts.Add(t switch
                {
                    CardType.Attack => "[red]攻[/red]",
                    CardType.Skill => "[blue]技[/blue]",
                    CardType.Power => "[gold]能[/gold]",
                    _ => "[pink]特[/pink]"
                });
            }
            return string.Join(" ", parts);
        }
    }
}