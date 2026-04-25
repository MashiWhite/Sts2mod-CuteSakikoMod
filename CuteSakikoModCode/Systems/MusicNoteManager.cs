
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Random;

namespace CuteSakikoMod.CuteSakikoModCode.Systems
{
    public static class MusicNoteManager
    {
        private class PlayerData
        {
            public Queue<CardType> Notes { get; } = new();
            public List<string> StoredChords { get; } = new();

            // 新增：本回合获得的音符总数（用于某些卡牌效果）
            public int NotesGainedThisTurn;
            public int LastRoundNumber; // 用于检测回合切换
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

        // ---------- 新版调用（含 Bonus 列表） ----------
        public static List<string> AddNote(Player player, CardType type,
            IReadOnlyDictionary<ChordCategory, string> learnedChords,
            IEnumerable<string> bonusChordIds)
        {
            if (player == null) return new List<string>();

            var data = GetData(player);

            // 跨回合自动清零
            var combat = player.Creature?.CombatState;
            int currentRound = combat?.RoundNumber ?? 0;
            if (data.LastRoundNumber != currentRound)
            {
                data.NotesGainedThisTurn = 0;
                data.LastRoundNumber = currentRound;
            }

            // 记录获得一个音符（无论是否匹配和弦）
            data.NotesGainedThisTurn++;

            // 原有逻辑…
            data.Notes.Enqueue(type);
            while (data.Notes.Count > 4)
                data.Notes.Dequeue();

            var sequence = data.Notes.ToList();
            var newChords = new List<string>();

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
        
        /// <summary>获取本回合已获得的音符总数</summary>
        public static int GetNotesGainedThisTurn(Player player)
        {
            if (player == null) return 0;
            // 调用此方法时也进行回合检查，防止回合切换后残留值
            var data = GetData(player);
            var combat = player.Creature?.CombatState;
            int currentRound = combat?.RoundNumber ?? 0;
            if (data.LastRoundNumber != currentRound)
            {
                data.NotesGainedThisTurn = 0;
                data.LastRoundNumber = currentRound;
            }
            return data.NotesGainedThisTurn;
        }

        /// <summary>随机移除一个音符队列中的音符。成功返回 true，音符为空返回 false。</summary>
        public static bool RemoveRandomNote(Player player, Rng rng)
        {
            if (player == null) return false;
            var data = GetData(player);
            if (data.Notes.Count == 0) return false;

            // 将队列转为数组，随机选一个移除，再重建队列
            var notesArray = data.Notes.ToArray();
            int removeIndex = rng.NextInt(notesArray.Length);
    
            data.Notes.Clear();
            for (int i = 0; i < notesArray.Length; i++)
            {
                if (i != removeIndex)
                    data.Notes.Enqueue(notesArray[i]);
            }
            return true;
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