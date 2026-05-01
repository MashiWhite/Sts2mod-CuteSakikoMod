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

    // ---------- 音符与储存和弦管理 ----------
    public static List<string> AddNote(Player player, CardType type,
        IReadOnlyDictionary<ChordCategory, string> learnedChords,
        IEnumerable<string> bonusChordIds)
    {
        if (player == null) return new List<string>();

        var data = GetData(player);

        // 跨回合自动清零
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
        var newChords = new List<string>();

        // 检查已学习和弦
        if (learnedChords != null)
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

        // 检查奖励和弦
        if (bonusChordIds != null)
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

        while (data.StoredChords.Count > MaxStoredChords)
            data.StoredChords.RemoveAt(0);

        // 触发“吉他主唱”效果（两端都执行，能力同步由 PowerCmd 保证）
        if (player?.Creature != null)
        {
            var vocalPower = player.Creature.GetPower<GuitarVocalPower>();
            if (vocalPower != null) _ = vocalPower.OnNoteGained(1);
        }

        return newChords;
    }

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
        // 两端都执行，依靠同步 Rng 保证结果相同
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