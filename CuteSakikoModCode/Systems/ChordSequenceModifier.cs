using MegaCrit.Sts2.Core.Entities.Cards;

namespace CuteSakikoMod.CuteSakikoModCode.Systems
{
    public abstract class ChordSequenceModifier
    {
        public abstract IReadOnlyList<CardType> Apply(IReadOnlyList<CardType> original);
    }

    // 替换指定位置的音符类型
    public class ReplaceNoteModifier : ChordSequenceModifier
    {
        public int Index { get; }
        public CardType NewType { get; }
        public ReplaceNoteModifier(int index, CardType newType)
        {
            Index = index;
            NewType = newType;
        }
        public override IReadOnlyList<CardType> Apply(IReadOnlyList<CardType> original)
        {
            var list = original.ToList();
            if (Index >= 0 && Index < list.Count)
                list[Index] = NewType;
            return list;
        }
    }

    // 删除某个位置的音符
    public class RemoveNoteModifier : ChordSequenceModifier
    {
        public int Index { get; }
        public RemoveNoteModifier(int index) => Index = index;
        public override IReadOnlyList<CardType> Apply(IReadOnlyList<CardType> original)
        {
            var list = original.ToList();
            if (Index >= 0 && Index < list.Count)
                list.RemoveAt(Index);
            return list;
        }
    }

    // 在指定位置插入音符
    public class InsertNoteModifier : ChordSequenceModifier
    {
        public int Index { get; }
        public CardType Type { get; }
        public InsertNoteModifier(int index, CardType type)
        {
            Index = index;
            Type = type;
        }
        public override IReadOnlyList<CardType> Apply(IReadOnlyList<CardType> original)
        {
            var list = original.ToList();
            int clamp = Math.Clamp(Index, 0, list.Count);
            list.Insert(clamp, Type);
            return list;
        }
    }

    // 将某个位置设为“任意”类型（通配符）
    public class WildcardNoteModifier : ChordSequenceModifier
    {
        public int Index { get; }
        public WildcardNoteModifier(int index) => Index = index;
        public override IReadOnlyList<CardType> Apply(IReadOnlyList<CardType> original)
        {
            var list = original.ToList();
            if (Index >= 0 && Index < list.Count)
                list[Index] = CardType.Status; // 用Status代表任意
            return list;
        }
    }
}