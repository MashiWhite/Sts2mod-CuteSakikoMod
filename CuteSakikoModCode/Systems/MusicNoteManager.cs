using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Random;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class MusicNoteManager
{
    public const int MaxStoredChords = 3;

    private static readonly Dictionary<Player, PlayerData> _data = new();

    // ---------- 新增：音符变化事件 ----------
    public static event Action<Player>? PlayerNotesChanged;
    // --------------------------------------

    private static PlayerData GetData(Player player)
    {
        if (!_data.TryGetValue(player, out var data))
        {
            data = new PlayerData();
            _data[player] = data;
        }

        return data;
    }
    
    public static async Task AddNoteAndAutoPlayAsync(
        Player player,
        CardType type,
        IReadOnlyDictionary<ChordCategory, string> learnedChords,
        IEnumerable<string> bonusChordIds,
        PlayerChoiceContext context)
    {
        var result = AddNote(player, type, learnedChords, bonusChordIds);

        var guitar = player?.Relics?.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar != null && context != null)
            await guitar.AutoPlayNewChords(context, result);
    }

    public static NoteProcessResult AddNote(
        Player player,
        CardType type,
        IReadOnlyDictionary<ChordCategory, string> learnedChords,
        IEnumerable<string> bonusChordIds,
        PlayerChoiceContext? context = null)
    {
        var result = new NoteProcessResult
        {
            NewChords = new List<string>(),
            OverflowChord = null,
            TotalStoredCount = 0
        };
        if (player == null) return result;

        var data = GetData(player);
        var combat = player.Creature?.CombatState;
        var currentRound = combat?.RoundNumber ?? 0;
        if (data.LastRoundNumber != currentRound)
        {
            data.NotesGainedThisTurn = 0;
            data.LastRoundNumber = currentRound;
        }

        data.NotesGainedThisTurn++;
        data.TotalNotesGainedThisCombat++;
        data.Notes.Enqueue(type);
        while (data.Notes.Count > 4)
            data.Notes.Dequeue();

        var sequence = data.Notes.ToList();
        var matchedChords = new List<string>();

        if (learnedChords != null)
            foreach (var kv in learnedChords)
            {
                var chordId = kv.Value;
                if (string.IsNullOrEmpty(chordId)) continue;
                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                {
                    var modifiedSeq = ChordSequenceModifierHelper.GetModifiedSequence(def, player.Creature);
                    if (ChordManager.MatchesChord(modifiedSeq, sequence))
                        matchedChords.Add(chordId);
                }
            }

        if (bonusChordIds != null)
            foreach (var chordId in bonusChordIds)
            {
                if (string.IsNullOrEmpty(chordId)) continue;
                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                {
                    var modifiedSeq = ChordSequenceModifierHelper.GetModifiedSequence(def, player.Creature);
                    if (ChordManager.MatchesChord(modifiedSeq, sequence))
                        matchedChords.Add(chordId);
                }
            }

        result.NewChords = matchedChords;

        foreach (var chordId in matchedChords)
            data.StoredChords.Add(chordId);

        while (data.StoredChords.Count > MaxStoredChords)
        {
            var overflow = data.StoredChords[0];
            data.StoredChords.RemoveAt(0);
            result.OverflowChord ??= overflow;
        }

        result.TotalStoredCount = data.StoredChords.Count;

        if (player?.Creature != null)
        {
            var vocalPower = player.Creature.GetPower<GuitarVocalPower>();
            if (vocalPower != null) _ = vocalPower.OnNoteGained(1);
        }

        if (context != null)
        {
            var guitar = player.Relics?.OfType<AnonGuitar>().FirstOrDefault();
            if (guitar != null)
                _ = guitar.AutoPlayNewChords(context, result);
        }

        // ★ 音符变化通知
        PlayerNotesChanged?.Invoke(player);
        return result;
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
        if (player == null) return false;
        var data = GetData(player);
        if (data.Notes.Count == 0) return false;
        var notesArray = data.Notes.ToArray();
        var removeIndex = rng.NextInt(notesArray.Length);
        data.Notes.Clear();
        for (var i = 0; i < notesArray.Length; i++)
            if (i != removeIndex)
                data.Notes.Enqueue(notesArray[i]);
        
        // ★ 音符变化通知
        PlayerNotesChanged?.Invoke(player);
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
        
        // ★ 音符变化通知
        PlayerNotesChanged?.Invoke(player);
    }

    public static void ClearCombatData(Player player)
    {
        if (player == null) return;
        if (_data.TryGetValue(player, out var data))
        {
            data.Notes.Clear();
            data.StoredChords.Clear();
            data.NotesGainedThisTurn = 0;
            data.TotalNotesGainedThisCombat = 0;
            data.LastRoundNumber = 0;
        }
        
        // ★ 音符变化通知
        PlayerNotesChanged?.Invoke(player);
    }

    public static void ClearAll(Player player)
    {
        if (player == null) return;
        _data.Remove(player);
        
        // ★ 音符变化通知
        PlayerNotesChanged?.Invoke(player);
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

    public static int GetTotalNotesGainedThisCombat(Player player)
    {
        if (player == null) return 0;
        var data = GetData(player);
        return data.TotalNotesGainedThisCombat;
    }

    public static int ClearNotesAndGetCount(Player player)
    {
        if (player == null) return 0;
        var data = GetData(player);
        var count = data.Notes.Count;
        data.Notes.Clear();
        
        // ★ 音符变化通知
        PlayerNotesChanged?.Invoke(player);
        return count;
    }
    
    /// <summary>
    /// 将所有当前音符全部修改为指定类型。如果没有音符则返回 false。
    /// </summary>
    public static bool ModifyAllNotes(Player player, CardType newType)
    {
        if (player == null) return false;
        var data = GetData(player);
        var count = data.Notes.Count;
        if (count == 0) return false;
        data.Notes.Clear();
        for (int i = 0; i < count; i++)
            data.Notes.Enqueue(newType);
        PlayerNotesChanged?.Invoke(player);
        return true;
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
        
        // ★ 音符变化通知
        PlayerNotesChanged?.Invoke(player);
        return true;
    }

    public static CardType? GetLastNote(Player player)
    {
        if (player == null) return null;
        var data = GetData(player);
        if (data.Notes.Count == 0) return null;
        return data.Notes.Last();
    }

    public struct NoteProcessResult
    {
        public List<string> NewChords;
        public string? OverflowChord;
        public int TotalStoredCount;
    }

    private class PlayerData
    {
        public int LastRoundNumber;
        public int NotesGainedThisTurn;
        public int TotalNotesGainedThisCombat;
        public Queue<CardType> Notes { get; } = new();
        public List<string> StoredChords { get; } = new();
    }
}