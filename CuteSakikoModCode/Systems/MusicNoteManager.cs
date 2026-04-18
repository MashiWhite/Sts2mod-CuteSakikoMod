using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace CuteSakikoMod.CuteSakikoModCode.Systems
{
    public static class MusicNoteManager
    {
        private class PlayerData
        {
            public Queue<CardType> Notes { get; } = new();
            public List<string> StoredChords { get; } = new(); // 改为 List，支持按内容移除
        }

        private static Dictionary<Player, PlayerData> _data = new();

        private static PlayerData GetData(Player player)
        {
            if (!_data.TryGetValue(player, out var data))
            {
                data = new PlayerData();
                _data[player] = data;
            }
            return data;
        }

        public const int MaxStoredChords = 3; // 战斗中最多储存 3 个和弦

        /// <summary>
        /// 添加音符，匹配已学习和弦，成功则加入全局储存队列。
        /// 返回本次新添加到 StoredChords 中的和弦 ID 列表（用于自动演奏等）。
        /// </summary>
        public static List<string> AddNote(Player player, CardType type,
            IReadOnlyDictionary<ChordCategory, string> learnedChords)
        {
            var data = GetData(player);
            data.Notes.Enqueue(type);
            while (data.Notes.Count > 4)
                data.Notes.Dequeue();

            var sequence = data.Notes.ToList();
            var newChords = new List<string>();

            foreach (var kv in learnedChords)
            {
                var chordId = kv.Value;
                if (string.IsNullOrEmpty(chordId)) continue;
                if (ChordManager.AllChords.TryGetValue(chordId, out var def) &&
                    ChordManager.MatchesChord(def, sequence))
                {
                    data.StoredChords.Add(chordId);
                    newChords.Add(chordId);
                    // 全局上限控制：超出上限时移除最早的和弦（索引0）
                    while (data.StoredChords.Count > MaxStoredChords)
                        data.StoredChords.RemoveAt(0);
                }
            }
            return newChords;
        }

        public static IReadOnlyList<CardType> GetCurrentNotes(Player player) =>
            GetData(player).Notes.ToList().AsReadOnly();

        public static IReadOnlyList<string> GetStoredChords(Player player) =>
            GetData(player).StoredChords.AsReadOnly();

        public static void ClearStoredChords(Player player) =>
            GetData(player).StoredChords.Clear();

        public static void ClearNotes(Player player) =>
            GetData(player).Notes.Clear();

        public static void ClearAll(Player player)
        {
            if (_data.ContainsKey(player))
                _data.Remove(player);
        }

        /// <summary>
        /// 从储存队列中移除指定和弦 ID 的一个实例（优先移除最近添加的）。
        /// 返回 true 表示成功移除，false 表示未找到。
        /// </summary>
        public static bool RemoveChord(Player player, string chordId)
        {
            var list = GetData(player).StoredChords;
            int index = list.FindLastIndex(c => c == chordId);
            if (index >= 0)
            {
                list.RemoveAt(index);
                return true;
            }
            return false;
        }
    }
}