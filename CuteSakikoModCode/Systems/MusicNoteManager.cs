using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Random;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class MusicNoteManager
{
    public const int MaxStoredChords = 3;

    private static readonly Dictionary<Player, PlayerData> _data = new();

    private static PlayerData GetData(Player player)
    {
        if (!_data.TryGetValue(player, out var data))
        {
            data = new PlayerData();
            _data[player] = data;
        }
        return data;
    }

    // ========== 新增结果结构体 ==========
    public struct NoteProcessResult
    {
        public List<string> NewChords;      // 新匹配到的和弦
        public string? OverflowChord;       // 因储存满而被挤出的最旧和弦
        public int TotalStoredCount;        // 添加后储存和弦的总数
    }

    // ========== 修改后的 AddNote ==========
    public static NoteProcessResult AddNote(
        Player player, CardType type,
        IReadOnlyDictionary<ChordCategory, string> learnedChords,
        IEnumerable<string> bonusChordIds)
    {
        var result = new NoteProcessResult
        {
            NewChords = new List<string>(),
            OverflowChord = null,
            TotalStoredCount = 0
        };

        if (player == null) return result;

        var data = GetData(player);

        // 回合重置
        var combat = player.Creature?.CombatState;
        var currentRound = combat?.RoundNumber ?? 0;
        if (data.LastRoundNumber != currentRound)
        {
            data.NotesGainedThisTurn = 0;
            data.LastRoundNumber = currentRound;
        }

        data.NotesGainedThisTurn++;
        data.Notes.Enqueue(type);
        while (data.Notes.Count > 4)
            data.Notes.Dequeue();

        var sequence = data.Notes.ToList();

        // 学习和弦匹配
        if (learnedChords != null)
        {
            foreach (var kv in learnedChords)
            {
                var chordId = kv.Value;
                if (string.IsNullOrEmpty(chordId)) continue;
                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                {
                    // 1. 获得修改后的序列（如果玩家身上有修改器Power）
                    var modifiedSeq = ChordSequenceModifierHelper.GetModifiedSequence(def, player.Creature);
                    // 2. 用修改后的序列匹配
                    if (ChordManager.MatchesChord(modifiedSeq, sequence))
                    {
                        data.StoredChords.Add(chordId);
                        result.NewChords.Add(chordId);
                    }
                }
            }
        }

        // 奖励和弦匹配
        if (bonusChordIds != null)
        {
            foreach (var chordId in bonusChordIds)
            {
                if (string.IsNullOrEmpty(chordId)) continue;
                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                {
                    // 1. 获得修改后的序列（如果玩家身上有修改器Power）
                    var modifiedSeq = ChordSequenceModifierHelper.GetModifiedSequence(def, player.Creature);
                    // 2. 用修改后的序列匹配
                    if (ChordManager.MatchesChord(modifiedSeq, sequence))
                    {
                        data.StoredChords.Add(chordId);
                        result.NewChords.Add(chordId);
                    }
                }
            }
        }

        // 溢出处理
        while (data.StoredChords.Count > MaxStoredChords)
        {
            result.OverflowChord = data.StoredChords[0];
            data.StoredChords.RemoveAt(0);
        }

        result.TotalStoredCount = data.StoredChords.Count;

        // 触发 GuitarVocalPower
        if (player?.Creature != null)
        {
            var vocalPower = player.Creature.GetPower<GuitarVocalPower>();
            if (vocalPower != null) _ = vocalPower.OnNoteGained(1);
        }

        return result;
    }

    // ========== 以下所有原方法保持不变 ==========
    public static int GetNotesGainedThisTurn(Player player)
    {
        if (player == null) return 0;
        var data = GetData(player);
        var combat = player.Creature?.CombatState;
        var currentRound = combat?.RoundNumber ?? 0;
        if (data.LastRoundNumber != currentRound)
        {
            data.NotesGainedThisTurn = 0;
            data.LastRoundNumber = currentRound;
        }
        return data.NotesGainedThisTurn;
    }

    public static bool RemoveRandomNote(Player player, Rng rng)
    {
        if (player == null) return false;
        var data = GetData(player);
        if (data.Notes.Count == 0) return false;
        var notesArray = data.Notes.ToArray();
        var removeIndex = rng.NextInt(notesArray.Length);
        data.Notes.Clear();
        for (var i = 0; i < notesArray.Length; i++)
            if (i != removeIndex)
                data.Notes.Enqueue(notesArray[i]);
        return true;
    }

    public static IReadOnlyList<CardType> GetCurrentNotes(Player player)
    {
        var data = GetData(player);
        return data.Notes.ToList().AsReadOnly();
    }

    public static IReadOnlyList<string> GetStoredChords(Player player)
    {
        var data = GetData(player);
        return data.StoredChords.AsReadOnly();
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
            data.NotesGainedThisTurn = 0;
            data.LastRoundNumber = 0;
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
        var data = GetData(player);
        var list = data.StoredChords;
        var index = list.FindLastIndex(c => c == chordId);
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
        var count = data.Notes.Count;
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

    private class PlayerData
    {
        public int LastRoundNumber;
        public int NotesGainedThisTurn;
        public Queue<CardType> Notes { get; } = new();
        public List<string> StoredChords { get; } = new();
    }
}