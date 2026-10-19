using System.Runtime.CompilerServices;
using CuteSakikoMod.CuteSakikoModCode.Nodes;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using STS2RitsuLib.Models.Capabilities;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Chord;

[RegisterModelCapability]
public class ChordStorageCapability : RelicCapability
{
    private static readonly ConditionalWeakTable<Player, PendingChordMigration> _pendingMigrationTable = new();

    private List<string> _bonusChords = new();
    private Dictionary<string, int> _chordCardBonus = new();
    private Dictionary<ChordCategory, List<string>> _equippedChords = new();
    private string _lastSyncedRaw = "";
    private List<string> _learnedChords = new();
    private List<string> _temporaryChords = new();

    // ==================== 宿主/配置 ====================
    public Player? HostPlayer => (Owner as RelicModel)?.Owner;

    private IChordDataStorage? HostData => Owner as IChordDataStorage;

    public IChordConfig Config => (Owner as IChordConfig) ?? DefaultChordConfig.Instance;

    public int FirstPlayBonus => Config.FirstPlayBonus;
    public int BaseChordBonus => Config.BaseChordBonus;
    public int MaxLearnedChordsPerCategory => Config.MaxLearnedChordsPerCategory;

    private sealed class DefaultChordConfig : IChordConfig
    {
        public static readonly DefaultChordConfig Instance = new();
    }

    // ==================== 迁移表 ====================
    public static bool TryGetPendingMigration(Player player, out PendingChordMigration data)
        => _pendingMigrationTable.TryGetValue(player, out data);

    public static void RemovePendingMigration(Player player)
        => _pendingMigrationTable.Remove(player);

    // ==================== 唯一性判断 ====================
    public bool IsActive
    {
        get
        {
            var player = HostPlayer;
            if (player?.Relics == null) return false;

            ChordStorageCapability? best = null;
            var bestPriority = int.MinValue;
            foreach (var relic in player.Relics)
            {
                var cap = relic.Capability<ChordStorageCapability>();
                if (cap == null) continue;
                var p = cap.Config.ChordPriority;
                if (p > bestPriority)
                {
                    bestPriority = p;
                    best = cap;
                }
            }
            return ReferenceEquals(best, this);
        }
    }

    // ==================== 数据访问 ====================
    public List<string> GetAllEquippedChords()
    {
        EnsureInitialized();
        var list = new List<string>();
        foreach (var cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
            if (_equippedChords.TryGetValue(cat, out var slots))
                list.AddRange(slots);
        list.AddRange(_bonusChords);
        list.AddRange(_temporaryChords);
        return list;
    }

    public List<string> GetEquippedChordIds(params ChordCategory[] categories)
    {
        EnsureInitialized();
        var result = new List<string>();
        var filter = categories.Length > 0 ? new HashSet<ChordCategory>(categories) : null;

        foreach (var cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
        {
            if (filter != null && !filter.Contains(cat)) continue;
            if (_equippedChords.TryGetValue(cat, out var slots))
                result.AddRange(slots);
        }

        if (filter == null || filter.Contains(ChordCategory.Bonus))
            result.AddRange(_bonusChords);
        if (_temporaryChords.Count > 0) result.AddRange(_temporaryChords);
        return result;
    }

    public IReadOnlyList<string> GetCategorySlots(ChordCategory category)
    {
        EnsureInitialized();
        return _equippedChords.TryGetValue(category, out var list)
            ? list.AsReadOnly()
            : new List<string>().AsReadOnly();
    }

    public IReadOnlyList<string> GetLearnedChords()
    {
        EnsureInitialized();
        return _learnedChords.AsReadOnly();
    }

    public IReadOnlyList<string> GetBonusChords()
    {
        EnsureInitialized();
        return _bonusChords.AsReadOnly();
    }

    public IReadOnlyList<string> GetTemporaryChords()
    {
        EnsureInitialized();
        return _temporaryChords.AsReadOnly();
    }

    public int GetMaxChordsPerCategory() => Config.MaxLearnedChordsPerCategory;

    public int GetDisplayBonus()
    {
        var player = HostPlayer;
        return player?.Creature?.CombatState != null
            ? ChordNoteSystem.GetDisplayBonus(player)
            : Config.BaseChordBonus;
    }

    // ==================== 数据修改 ====================
    public void AddEquippedChord(ChordCategory category, string chordId)
    {
        EnsureInitialized();
        if (!_equippedChords.ContainsKey(category)) return;
        if (_equippedChords[category].Count >= Config.MaxLearnedChordsPerCategory) return;
        _equippedChords[category].Add(chordId);
        AddToLearnedIfMissing(chordId);
        Flash();
        SyncToSaved();
    }

    public void ReplaceEquippedChord(ChordCategory category, int index, string newChordId)
    {
        EnsureInitialized();
        if (!_equippedChords.ContainsKey(category) || index < 0 || index >= _equippedChords[category].Count) return;
        _equippedChords[category][index] = newChordId;
        AddToLearnedIfMissing(newChordId);
        Flash();
        SyncToSaved();
    }

    public bool RemoveEquippedChord(ChordCategory category, string chordId)
    {
        EnsureInitialized();
        if (!_equippedChords.ContainsKey(category)) return false;
        if (!_equippedChords[category].Remove(chordId)) return false;
        SyncToSaved();
        Flash();
        return true;
    }

    public void AddBonusChord(string chordId)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(chordId)) return;
        _bonusChords.Add(chordId);
        AddToLearnedIfMissing(chordId);
        Flash();
        SyncToSaved();
    }

    public bool RemoveBonusChord(string chordId)
    {
        EnsureInitialized();
        if (!_bonusChords.Remove(chordId)) return false;
        Flash();
        SyncToSaved();
        return true;
    }

    public void AddTemporaryChord(string chordId)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(chordId)) return;
        _temporaryChords.Add(chordId);
        Flash();
        SyncToSaved();
    }

    public bool RemoveTemporaryChord(string chordId)
    {
        EnsureInitialized();
        if (!_temporaryChords.Remove(chordId)) return false;
        Flash();
        SyncToSaved();
        return true;
    }

    public void ClearTemporaryChords()
    {
        EnsureInitialized();
        if (_temporaryChords.Count == 0) return;
        _temporaryChords.Clear();
        Flash();
        SyncToSaved();
    }

    public void LearnChord(string chordId)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(chordId)) return;
        if (!ChordManager.AllChords.TryGetValue(chordId, out var def)) return;

        // ★ 过滤：
        //   1. 临时专属和弦（AddTemporaryChord 时打标记）不进学习池
        //      —— 例如 AnonCChord / AnonDChord / HekitenbansouChord / GreyAnonChord
        //   2. 类别为 Anon 的和弦不进学习池（双保险）
        //      —— 即使某个临时和弦忘了打 IsTemporaryOnly 标记，也能被拦住
        if (def.IsTemporaryOnly) return;
        if (def.Category == ChordCategory.Anon) return;

        AddToLearnedIfMissing(chordId);
        SyncToSaved();
        Flash();
    }

    public void AutoLearnChordOnRest()
    {
        EnsureInitialized();
        var player = HostPlayer;
        if (player?.RunState == null) return;
        var pool = new List<string>();
        foreach (var cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
            pool.AddRange(ChordManager.GetLearnableChordIds(cat));
        var available = pool.Where(id => !_learnedChords.Contains(id) && !ChordManager.AllChords[id].IsTemporaryOnly)
            .ToList();
        if (available.Count == 0) return;
        var newChord = player.RunState.Rng.Niche.NextItem(available);
        _learnedChords.Add(newChord);
        SyncToSaved();
        Flash();
    }

    public void FillCategorySlots(ChordCategory category)
    {
        EnsureInitialized();
        var player = HostPlayer;
        if (player == null) return;
        var targetCount = Config.MaxLearnedChordsPerCategory;
        if (!_equippedChords.ContainsKey(category))
            _equippedChords[category] = new List<string>();
        var slots = _equippedChords[category];
        while (slots.Count < targetCount)
        {
            var available = ChordManager.GetLearnableChordIds(category)
                .Where(id => !_learnedChords.Contains(id) && !ChordManager.AllChords[id].IsTemporaryOnly)
                .ToList();
            if (available.Count == 0) break;
            var newChord = player.RunState.Rng.UpFront.NextItem(available);
            AddEquippedChord(category, newChord);
        }
    }

    public void ReplaceRandomEquippedChord(string newChordId)
    {
        EnsureInitialized();
        if (!ChordManager.AllChords.ContainsKey(newChordId)) return;

        var availableCategories = _equippedChords
            .Where(kv => kv.Value.Count > 0)
            .Select(kv => kv.Key)
            .ToList();
        if (availableCategories.Count == 0) return;

        var player = HostPlayer;
        if (player == null) return;
        var rng = player.RunState.Rng.Niche;
        var targetCategory = rng.NextItem(availableCategories);
        var targetList = _equippedChords[targetCategory];
        var targetIndex = rng.NextInt(targetList.Count);

        ReplaceEquippedChord(targetCategory, targetIndex, newChordId);
    }

    private void AddToLearnedIfMissing(string chordId)
    {
        if (!_learnedChords.Contains(chordId))
            _learnedChords.Add(chordId);
    }

    // ==================== 和弦卡升级加成 ====================
    public int GetChordCardBonus(string chordId)
    {
        EnsureInitialized();
        return _chordCardBonus.TryGetValue(chordId, out var b) ? b : 0;
    }

    public void ClearChordCardBonus()
    {
        EnsureInitialized();
        if (_chordCardBonus.Count == 0) return;
        _chordCardBonus.Clear();
        SyncToSaved();
    }

    public void AddChordCardBonus(string chordId, int amount)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(chordId) || amount <= 0) return;
        _chordCardBonus[chordId] = _chordCardBonus.GetValueOrDefault(chordId) + amount;
        SyncToSaved();
    }

    // ==================== 初始化与序列化 ====================
    public void InvalidateCache() => _lastSyncedRaw = "";

    public void EnsureInitialized()
    {
        var data = HostData;
        if (data == null) return;

        var raw =
            $"{data.SavedChordsData}|{data.SavedBonusChordsData}|{data.SavedTemporaryChordsData}|{data.SavedLearnedChordsData}|{data.SavedChordCardBonusData}";
        if (_lastSyncedRaw == raw) return;
        _lastSyncedRaw = raw;

        _equippedChords = new Dictionary<ChordCategory, List<string>>
        {
            { ChordCategory.Major, new List<string>() },
            { ChordCategory.Minor, new List<string>() },
            { ChordCategory.Dominant, new List<string>() }
        };
        _bonusChords = new List<string>();
        _temporaryChords = new List<string>();
        var hasAnyData = false;

        if (!string.IsNullOrEmpty(data.SavedChordsData))
        {
            foreach (var pair in data.SavedChordsData.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out var catInt) &&
                    Enum.IsDefined(typeof(ChordCategory), catInt) && (ChordCategory)catInt != ChordCategory.Bonus)
                    _equippedChords[(ChordCategory)catInt].Add(parts[1]);
            }
            hasAnyData = true;
        }

        if (!string.IsNullOrEmpty(data.SavedBonusChordsData))
        {
            _bonusChords = data.SavedBonusChordsData.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            hasAnyData = true;
        }

        if (!string.IsNullOrEmpty(data.SavedTemporaryChordsData))
            _temporaryChords = data.SavedTemporaryChordsData.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();

        _chordCardBonus = new Dictionary<string, int>();
        if (!string.IsNullOrEmpty(data.SavedChordCardBonusData))
        {
            foreach (var pair in data.SavedChordCardBonusData.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out var amt))
                    _chordCardBonus[parts[0]] = amt;
            }
        }

        if (!hasAnyData)
        {
            _equippedChords[ChordCategory.Major].Add("C");
            _equippedChords[ChordCategory.Minor].Add("Cm");
            _equippedChords[ChordCategory.Dominant].Add("C7");
        }

        if (!string.IsNullOrEmpty(data.SavedLearnedChordsData))
        {
            _learnedChords = data.SavedLearnedChordsData.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
        else
        {
            _learnedChords = _equippedChords.Values.SelectMany(l => l).Concat(_bonusChords).Distinct().ToList();
            if (!_learnedChords.Contains("C")) _learnedChords.Add("C");
            if (!_learnedChords.Contains("Cm")) _learnedChords.Add("Cm");
            if (!_learnedChords.Contains("C7")) _learnedChords.Add("C7");
        }

        SyncToSaved();
    }

    public void SyncToSaved()
    {
        var data = HostData;
        if (data == null) return;

        var chordParts = new List<string>();
        foreach (var cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
            if (_equippedChords.TryGetValue(cat, out var slots))
                foreach (var id in slots)
                    chordParts.Add($"{(int)cat}:{id}");

        data.SavedChordsData = string.Join(";", chordParts);
        data.SavedBonusChordsData = string.Join(";", _bonusChords);
        data.SavedTemporaryChordsData = string.Join(";", _temporaryChords);
        data.SavedLearnedChordsData = string.Join(";", _learnedChords);
        data.SavedChordCardBonusData = string.Join(";",
            _chordCardBonus.Where(kv => kv.Value != 0).Select(kv => $"{kv.Key}:{kv.Value}"));

        _lastSyncedRaw =
            $"{data.SavedChordsData}|{data.SavedBonusChordsData}|{data.SavedTemporaryChordsData}|{data.SavedLearnedChordsData}|{data.SavedChordCardBonusData}";

        var player = HostPlayer;
        if (player != null)
        {
            var mig = _pendingMigrationTable.GetOrCreateValue(player);
            mig.Chords = data.SavedChordsData;
            mig.Bonus = data.SavedBonusChordsData;
            mig.Temp = data.SavedTemporaryChordsData;
            mig.BonusChords = new List<string>(_bonusChords);
        }
    }

    public void SetLearnedChordsFromString(string data)
    {
        var host = HostData;
        if (host == null) return;
        host.SavedLearnedChordsData = data;
        _lastSyncedRaw = "";
        EnsureInitialized();
        SyncToSaved();
        Flash();
    }

    public void RestoreChordData(string chordsData, string bonusData, string tempData)
    {
        var host = HostData;
        if (host == null) return;
        host.SavedChordsData = chordsData;
        host.SavedBonusChordsData = bonusData;
        host.SavedTemporaryChordsData = tempData;
        _lastSyncedRaw = "";
        EnsureInitialized();
        SyncToSaved();
        Flash();
    }

    public void CopyTo(ChordStorageCapability target)
    {
        EnsureInitialized();
        var src = HostData;
        var dst = target.HostData;
        if (src == null || dst == null) return;

        dst.SavedChordsData = src.SavedChordsData;
        dst.SavedBonusChordsData = src.SavedBonusChordsData;
        dst.SavedTemporaryChordsData = src.SavedTemporaryChordsData;
        dst.SavedLearnedChordsData = src.SavedLearnedChordsData;
        dst.SavedChordCardBonusData = src.SavedChordCardBonusData;
        target._lastSyncedRaw = "";
        target.EnsureInitialized();
        target.SyncToSaved();
    }

    // ==================== 生命周期处理方法 ====================
    public async Task HandleAfterObtained()
    {
        if (!IsActive) return;
        EnsureInitialized();
        var player = HostPlayer;
        if (player?.Creature?.CombatState != null)
            ChordNoteSystem.Activate(player);
        await Task.CompletedTask;
    }

    public async Task HandleBeforeCombatStart()
    {
        if (!IsActive) return;
        EnsureInitialized();
        var player = HostPlayer;
        if (player == null) return;
        ChordNoteSystem.Activate(player);
        await ChordCardManager.ProcessPlayerChordCardsAsync(player);
    }

    public async Task HandleAfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (!IsActive) return;
        if (player != HostPlayer) return;
        ChordNoteSystem.OnPlayerTurnStart(player);
        await Task.CompletedTask;
    }

    public async Task HandleAfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!IsActive) return;
        var player = HostPlayer;
        if (player == null) return;
        if (cardPlay.Card.Owner != player) return;
        if (player.Creature.CombatState == null) return;

        if (CutesakiKeywords.NoNote != null &&
            cardPlay.Card.Keywords.Contains(CutesakiKeywords.NoNote.GetModCardKeyword()))
        {
            ChordNoteUIManager.UpdateNoteDisplay(player);
            ChordNoteUIManager.UpdateStoredChordDisplay(player);
            return;
        }

        await ChordNoteSystem.AddNoteAsync(player, cardPlay.Card.Type, choiceContext);
        await HandleMessyPlay(choiceContext, player);
        ChordNoteUIManager.UpdateNoteDisplay(player);
        ChordNoteUIManager.UpdateStoredChordDisplay(player);
    }

    public async Task HandleAfterRoomEntered(AbstractRoom room)
    {
        if (!IsActive) return;
        if (room is RestSiteRoom) AutoLearnChordOnRest();
        await Task.CompletedTask;
    }

    public async Task HandleAfterCombatEnd(CombatRoom room)
    {
        if (!IsActive) return;
        ClearTemporaryChords();
        var player = HostPlayer;
        if (player != null)
        {
            ChordNoteSystem.OnCombatEnd(player);
            ChordSequenceModifierHelper.ClearCardModifiers(player);
        }
        SyncToSaved();
        await Task.CompletedTask;
    }

    private async Task HandleMessyPlay(PlayerChoiceContext choiceContext, Player player)
    {
        var messyPlay = player.Creature?.GetPower<MessyPlayPower>();
        if (messyPlay == null || messyPlay.Amount <= 0) return;
        if (!messyPlay.OnNoteObtained()) return;

        messyPlay.StartGeneratingNotes();
        var combat = player.Creature!.CombatState;
        if (combat != null)
        {
            var possibleTypes = new[] { CardType.Attack, CardType.Skill, CardType.Power };
            var rng = combat.RunState.Rng.CombatCardSelection;
            for (var i = 0; i < messyPlay.Amount; i++)
                await OnNoteGenerated(choiceContext, player, rng.NextItem(possibleTypes), triggerEffect: true);
        }

        messyPlay.ResetNoteCount();
        messyPlay.EndGeneratingNotes();
    }

    public async Task OnNoteGenerated(PlayerChoiceContext choiceContext, Player? player, CardType noteType,
        bool triggerEffect = false)
    {
        player ??= HostPlayer;
        if (player?.Creature?.CombatState == null) return;

        await ChordNoteSystem.AddNoteAsync(player, noteType, choiceContext, triggerEffect);
        await HandleMessyPlay(choiceContext, player);
        ChordNoteUIManager.UpdateNoteDisplay(player);
        ChordNoteUIManager.UpdateStoredChordDisplay(player);
    }

    // ==================== 悬浮提示构建 ====================
    public IEnumerable<IHoverTip> BuildHoverTips()
    {
        // canonical 上下文（预览/图鉴）没有玩家，直接跳过
        if (!IsMutable) yield break;

        if (HostPlayer == null) yield break;

        var desc = new LocString("relics", "CUTE_SAKIKO_MOD_RELIC_ANON_GUITAR_CHORDS_DESC");
        var lines = new List<string>();

        foreach (var cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
        foreach (var chordId in GetCategorySlots(cat))
            AppendChordLine(lines, chordId, "");

        foreach (var chordId in GetBonusChords())
            AppendChordLine(lines, chordId, "");

        foreach (var chordId in GetTemporaryChords())
            AppendChordLine(lines, chordId, "[临时] ");

        desc.Add("Chords", string.Join("\n\n", lines));
        yield return new HoverTip(
            new LocString("relics", "CUTE_SAKIKO_MOD_RELIC_ANON_GUITAR_CHORDS_TITLE"), desc);
    }

    private void AppendChordLine(List<string> lines, string chordId, string prefix)
    {
        if (!ChordManager.AllChords.TryGetValue(chordId, out var def)) return;
        var title = new LocString("card_keywords", def.TitleKey).GetFormattedText();
        var text = ChordDisplayHelper.GetFormattedDescription(def, GetDisplayBonus());
        var owner = HostPlayer?.Creature;
        var condition = owner != null
            ? ChordSequenceModifierHelper.GetModifiedConditionText(def, owner)
            : def.GetConditionText();
        lines.Add($"{prefix}[{title}]({condition})\n{text}");
    }

    // ==================== 右键界面入口 ====================
    public void OpenManagementScreen(bool readOnly)
    {
        var screen = new ChordManagementScreen();
        screen.SetChords(this);
        screen.SetReadOnly(readOnly);
        screen.ShowScreen();
    }

    // ==================== 内部工具 ====================
    public void Flash()
    {
        if (Owner is RelicModel relic)
            relic.Flash();
    }
}