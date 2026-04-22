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
            public List<string> StoredChords { get; } = new();
        }

        private static Dictionary<Player, PlayerData> _data = new();
        public const int MaxStoredChords = 3;

        private static PlayerData GetData(Player player)
        {
            if (!_data.TryGetValue(player, out var data))
            {
                data = new PlayerData();
                _data[player] = data;
            }
            return data;
        }

        // ---------- 兼容旧版调用（无 Bonus 参数） ----------
        public static List<string> AddNote(Player player, CardType type,
            IReadOnlyDictionary<ChordCategory, string> learnedChords)
        {
            return AddNote(player, type, learnedChords, Enumerable.Empty<string>());
        }

        // ---------- 新版调用（含 Bonus 列表） ----------
        public static List<string> AddNote(Player player, CardType type,
            IReadOnlyDictionary<ChordCategory, string> learnedChords,
            IEnumerable<string> bonusChordIds)
        {
            if (player == null) return new List<string>();
            
            var data = GetData(player);
            data.Notes.Enqueue(type);
            while (data.Notes.Count > 4)
                data.Notes.Dequeue();

            var sequence = data.Notes.ToList();
            var newChords = new List<string>();

            // 匹配主槽位
            if (learnedChords != null)
            {
                foreach (var kv in learnedChords)
                {
                    var chordId = kv.Value;
                    if (string.IsNullOrEmpty(chordId)) continue;
                    if (ChordManager.AllChords.TryGetValue(chordId, out var def) &&
                        ChordManager.MatchesChord(def, sequence))
                    {
                        data.StoredChords.Add(chordId);
                        newChords.Add(chordId);
                    }
                }
            }

            // 匹配 Bonus 槽位
            if (bonusChordIds != null)
            {
                foreach (var chordId in bonusChordIds)
                {
                    if (string.IsNullOrEmpty(chordId)) continue;
                    if (ChordManager.AllChords.TryGetValue(chordId, out var def) &&
                        ChordManager.MatchesChord(def, sequence))
                    {
                        data.StoredChords.Add(chordId);
                        newChords.Add(chordId);
                    }
                }
            }

            while (data.StoredChords.Count > MaxStoredChords)
                data.StoredChords.RemoveAt(0);

            return newChords;
        }

        // ---------- 其他原有方法保持不变 ----------
        public static IReadOnlyList<CardType> GetCurrentNotes(Player player)
        {
            if (player == null) return new List<CardType>();
            return GetData(player).Notes.ToList().AsReadOnly();
        }

        public static IReadOnlyList<string> GetStoredChords(Player player)
        {
            if (player == null) return new List<string>();
            return GetData(player).StoredChords.AsReadOnly();
        }

        public static void ClearStoredChords(Player player)
        {
            if (player == null) return;
            GetData(player).StoredChords.Clear();
        }

        public static void ClearNotes(Player player)
        {
            if (player == null) return;
            GetData(player).Notes.Clear();
        }

        public static void ClearCombatData(Player player)
        {
            if (player == null) return;
            if (_data.TryGetValue(player, out var data))
            {
                data.Notes.Clear();
                data.StoredChords.Clear();
            }
        }

        public static void ClearAll(Player player)
        {
            if (player == null) return;
            _data.Remove(player);
        }

        public static bool RemoveChord(Player player, string chordId)
        {
            if (player == null) return false;
            var list = GetData(player).StoredChords;
            int index = list.FindLastIndex(c => c == chordId);
            if (index >= 0)
            {
                list.RemoveAt(index);
                return true;
            }
            return false;
        }

        public static void AddChordDirectly(Player player, string chordId)
        {
            if (player == null) return;
            var data = GetData(player);
            data.StoredChords.Add(chordId);
            while (data.StoredChords.Count > MaxStoredChords)
                data.StoredChords.RemoveAt(0);
        }

        public static int ClearNotesAndGetCount(Player player)
        {
            if (player == null) return 0;
            var data = GetData(player);
            int count = data.Notes.Count;
            data.Notes.Clear();
            return count;
        }

        public static bool ModifyLastNote(Player player, CardType newType)
        {
            if (player == null) return false;
            var data = GetData(player);
            if (data.Notes.Count == 0) return false;

            var notesArray = data.Notes.ToArray();
            notesArray[^1] = newType;

            data.Notes.Clear();
            foreach (var note in notesArray)
                data.Notes.Enqueue(note);

            return true;
        }

        public static CardType? GetLastNote(Player player)
        {
            if (player == null) return null;
            var data = GetData(player);
            if (data.Notes.Count == 0) return null;
            return data.Notes.Last();
        }
    }
}