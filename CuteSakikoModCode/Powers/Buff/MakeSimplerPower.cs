using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Random;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public class MakeSimplerPower : CuteSakikoModPower, IChordSequenceModifierProvider
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;  // ★ 可叠层
    public IEnumerable<ChordCategory>? AffectedCategories => null;       // 影响所有类别

    public IEnumerable<ChordSequenceModifier> GetModifiers(Creature owner, ChordDefinition chordDef)
    {
        if (Amount <= 0) yield break;

        // 检查该和弦是否属于拥有吉他玩家的已记忆和弦
        var guitar = owner.Player?.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) yield break;

        var allChordIds = guitar.GetEquippedChordIds();
        if (!allChordIds.Contains(chordDef.Id)) yield break;  // 只处理自己拥有的和弦

        int noteCount = chordDef.NoteSequence.Length;
        if (noteCount == 0) yield break;

        int replaceCount = Math.Min(Amount, noteCount);  // 最多替换所有音符

        // 使用稳定的随机序列（基于和弦ID和战斗RNG），确保每次查询返回相同位置
        var rng = owner.CombatState?.RunState.Rng.CombatCardGeneration;
        if (rng == null)
        {
            // 没有RNG时按顺序替换前面的音符
            for (int i = 0; i < replaceCount; i++)
                yield return new ReplaceNoteModifier(i, Entry.AnyNote);
        }
        else
        {
            // 生成随机不重复位置
            var indices = Enumerable.Range(0, noteCount).ToList();
            // 使用 Fisher-Yates 打乱（基于和弦ID的确定性随机）
            var seededRng = new DeterministicRng(rng, chordDef.Id.GetHashCode());
            for (int i = indices.Count - 1; i > 0; i--)
            {
                int j = seededRng.NextInt(i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            for (int i = 0; i < replaceCount; i++)
            {
                yield return new ReplaceNoteModifier(indices[i], Entry.AnyNote);
            }
        }
    }

    // 一个简单的确定性随机数生成器，用于保证和弦修改器的顺序一致
    private class DeterministicRng
    {
        private int _state;
        public DeterministicRng(Rng baseRng, int seed)
        {
            _state = (int)(baseRng.Seed ^ (uint)seed);
        }
        public int NextInt(int max)
        {
            _state = (int)(((uint)_state * 1103515245 + 12345) & 0x7fffffff);
            return _state % max;
        }
    }
}