using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Nodes;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;

[RegisterCharacterStarterRelic(typeof(CuteAnon))]
[RegisterTouchOfOrobasRefinement(typeof(FlashAnonGuitar))]
public class AnonGuitar : CuteAnonRelic, IChordProvider, IModRightClickableRelic
{
    // ==================== 静态字段 ====================
    protected static Dictionary<Player, (string chords, string bonus, string temp)> _pendingMigration = new();
    protected static Dictionary<Player, List<string>> _pendingBonusMigration = new();

    // ==================== 实例字段 ====================
    protected Dictionary<ChordCategory, List<string>> _equippedChords = new();
    protected List<string> _learnedChords = new();
    protected List<string> _bonusChords = new();
    protected List<string> _temporaryChords = new();
    protected bool _initialized;

    // 序列化字段
    protected string _savedChordsData = "";
    protected string _savedBonusChordsData = "";
    protected string _savedTemporaryChordsData = "";
    protected string _savedLearnedChordsData = "";

    // ==================== 属性 ====================
    public override RelicRarity Rarity => RelicRarity.Starter;
    public virtual int FirstPlayBonus => 1;
    public virtual int BaseChordBonus => 0;
    protected virtual int MaxLearnedChordsPerCategory => 1;

    protected override IEnumerable<string> RegisteredKeywordIds => [CutesakiKeywords.RememberChord];

    [SavedProperty]
    protected string SavedChordsData
    {
        get => _savedChordsData;
        set => _savedChordsData = value;
    }

    [SavedProperty]
    protected string SavedBonusChordsData
    {
        get => _savedBonusChordsData;
        set => _savedBonusChordsData = value;
    }

    [SavedProperty]
    protected string SavedTemporaryChordsData
    {
        get => _savedTemporaryChordsData;
        set => _savedTemporaryChordsData = value;
    }

    [SavedProperty]
    protected string SavedLearnedChordsData
    {
        get => _savedLearnedChordsData;
        set => _savedLearnedChordsData = value;
    }

    // ==================== 悬停提示 ====================
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (!IsMutable) yield break;
            var desc = new LocString("relics", "CUTE_SAKIKO_MOD_RELIC_ANON_GUITAR_CHORDS_DESC");
            var lines = new List<string>();

            foreach (var cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
            foreach (var chordId in _equippedChords.GetValueOrDefault(cat, new List<string>()))
                AppendChordLine(lines, chordId, "CUTE_SAKIKO_MOD_RELIC_ANON_GUITAR_CHORDS_TITLE", cat);

            foreach (var chordId in _bonusChords)
                AppendChordLine(lines, chordId, null, null);

            foreach (var chordId in _temporaryChords)
                AppendChordLine(lines, chordId, null, null, "[临时] ");

            desc.Add("Chords", string.Join("\n\n", lines));
            yield return new HoverTip(new LocString("relics", "CUTE_SAKIKO_MOD_RELIC_ANON_GUITAR_CHORDS_TITLE"), desc);
        }
    }

    private void AppendChordLine(List<string> lines, string chordId, string? titleKey, ChordCategory? cat, string prefix = "")
    {
        if (!ChordManager.AllChords.TryGetValue(chordId, out var def)) return;
        var title = new LocString("card_keywords", def.TitleKey).GetFormattedText();
        var text = ChordDisplayHelper.GetFormattedDescription(def, GetDisplayBonus());
        var condition = ChordSequenceModifierHelper.GetModifiedConditionText(def, Owner.Creature);
        lines.Add($"{prefix}[{title}]({condition})\n{text}");
    }

    public int GetDisplayBonus()
    {
        return Owner?.Creature?.CombatState != null
            ? ChordNoteSystem.GetDisplayBonus(Owner)
            : BaseChordBonus;
    }

    // ==================== 右键菜单 ====================
    public bool CanHandleRightClickLocal(ModRightClickContext context) => true;

    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        var me = LocalContext.GetMe(RunManager.Instance.DebugOnlyGetState()?.Players);
        if (me == null || me.NetId != Owner.NetId) return;

        var screen = new ChordManagementScreen();
        screen.SetGuitar(this);
        screen.SetReadOnly(true);
        screen.ShowScreen();
        await Task.CompletedTask;
    }

    // ==================== 生命周期 ====================
    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        EnsureInitialized();
        if (Owner?.Creature?.CombatState != null)
        {
            ChordNoteSystem.Activate(Owner);
            ChordNoteSystem.RegisterProvider(Owner, this);
        }
    }

    public override async Task BeforeCombatStart()
    {
        await base.BeforeCombatStart();
        ChordNoteSystem.Activate(Owner);
        ChordNoteSystem.RegisterProvider(Owner, this);
    }

    public override async Task AfterRemoved()
    {
        ChordNoteSystem.UnregisterProvider(Owner, this);
        await base.AfterRemoved();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        ChordNoteSystem.OnPlayerTurnStart(player);
        await Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        EnsureInitialized();
        if (cardPlay.Card.Owner != Owner) return;
        if (Owner.Creature.CombatState == null) return;

        if (CutesakiKeywords.NoNote != null &&
            cardPlay.Card.Keywords.Contains(CutesakiKeywords.NoNote.GetModCardKeyword()))
        {
            ChordNoteUIManager.UpdateNoteDisplay(Owner);
            ChordNoteUIManager.UpdateStoredChordDisplay(Owner);
            return;
        }

        await ChordNoteSystem.AddNoteAsync(Owner, cardPlay.Card.Type, choiceContext);

        await HandleMessyPlay(choiceContext);
        ChordNoteUIManager.UpdateNoteDisplay(Owner);
        ChordNoteUIManager.UpdateStoredChordDisplay(Owner);
    }

    private async Task HandleMessyPlay(PlayerChoiceContext choiceContext)
    {
        var messyPlay = Owner.Creature?.GetPower<MessyPlayPower>();
        if (messyPlay == null || messyPlay.Amount <= 0) return;
        if (!messyPlay.OnNoteObtained()) return;

        messyPlay.StartGeneratingNotes();
        var combat = Owner.Creature!.CombatState;
        if (combat != null)
        {
            var possibleTypes = new[] { CardType.Attack, CardType.Skill, CardType.Power };
            var rng = combat.RunState.Rng.CombatCardSelection;
            for (var i = 0; i < messyPlay.Amount; i++)
                await OnNoteGenerated(choiceContext, rng.NextItem(possibleTypes));
        }
        messyPlay.ResetNoteCount();
        messyPlay.EndGeneratingNotes();
    }

    public async Task OnNoteGenerated(PlayerChoiceContext choiceContext, CardType noteType)
    {
        if (Owner.Creature.CombatState == null) return;
        await ChordNoteSystem.AddNoteAsync(Owner, noteType, choiceContext);
        await HandleMessyPlay(choiceContext);
        ChordNoteUIManager.UpdateNoteDisplay(Owner);
        ChordNoteUIManager.UpdateStoredChordDisplay(Owner);
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        await base.AfterRoomEntered(room);
        if (room is RestSiteRoom) AutoLearnChordOnRest();
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        ClearTemporaryChords();
        ChordNoteSystem.OnCombatEnd(Owner);
        ChordSequenceModifierHelper.ClearCardModifiers(Owner);
        SyncToSaved();
        await base.AfterCombatEnd(room);
    }

    // ==================== IChordProvider ====================
    public IReadOnlyList<string> GetAvailableChordIds(Player player)
    {
        EnsureInitialized();
        return GetAllEquippedChords();
    }

    // ==================== 数据访问 ====================
    public List<string> GetAllEquippedChords()
    {
        EnsureInitialized();
        var list = new List<string>();
        foreach (var kv in _equippedChords)
            list.AddRange(kv.Value);
        list.AddRange(_bonusChords);
        list.AddRange(_temporaryChords);
        return list;
    }

    public List<string> GetEquippedChordIds(params ChordCategory[] categories)
    {
        EnsureInitialized();
        var result = new List<string>();
        var filter = categories.Length > 0 ? new HashSet<ChordCategory>(categories) : null;
        foreach (var kv in _equippedChords)
            if (filter == null || filter.Contains(kv.Key))
                result.AddRange(kv.Value);
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

    public int GetMaxChordsPerCategory() => MaxLearnedChordsPerCategory;

    // ==================== 数据修改 ====================
    public void AddEquippedChord(ChordCategory category, string chordId)
    {
        EnsureInitialized();
        if (!_equippedChords.ContainsKey(category)) return;
        if (_equippedChords[category].Count >= MaxLearnedChordsPerCategory) return;
        _equippedChords[category].Add(chordId);
        AddToLearnedIfMissing(chordId);
        if (Owner != null) Flash();
        SyncToSaved();
    }

    public void ReplaceEquippedChord(ChordCategory category, int index, string newChordId)
    {
        EnsureInitialized();
        if (!_equippedChords.ContainsKey(category) || index < 0 || index >= _equippedChords[category].Count) return;
        _equippedChords[category][index] = newChordId;
        AddToLearnedIfMissing(newChordId);
        if (Owner != null) Flash();
        SyncToSaved();
    }

    public bool RemoveEquippedChord(ChordCategory category, string chordId)
    {
        EnsureInitialized();
        if (!_equippedChords.ContainsKey(category)) return false;
        if (!_equippedChords[category].Remove(chordId)) return false;
        SyncToSaved();
        if (Owner != null) Flash();
        return true;
    }

    public void AddBonusChord(string chordId)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(chordId)) return;
        _bonusChords.Add(chordId);
        AddToLearnedIfMissing(chordId);
        if (Owner != null) Flash();
        SyncToSaved();
    }

    public bool RemoveBonusChord(string chordId)
    {
        EnsureInitialized();
        if (!_bonusChords.Remove(chordId)) return false;
        if (Owner != null) Flash();
        SyncToSaved();
        return true;
    }

    public void AddTemporaryChord(string chordId)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(chordId)) return;
        _temporaryChords.Add(chordId);
        if (Owner != null) Flash();
        SyncToSaved();
    }

    public bool RemoveTemporaryChord(string chordId)
    {
        EnsureInitialized();
        if (!_temporaryChords.Remove(chordId)) return false;
        if (Owner != null) Flash();
        SyncToSaved();
        return true;
    }

    public void ClearTemporaryChords()
    {
        EnsureInitialized();
        if (_temporaryChords.Count == 0) return;
        _temporaryChords.Clear();
        if (Owner != null) Flash();
        SyncToSaved();
    }

    public void LearnChord(string chordId)
    {
        EnsureInitialized();
        if (string.IsNullOrEmpty(chordId)) return;
        if (!ChordManager.AllChords.ContainsKey(chordId)) return;
        AddToLearnedIfMissing(chordId);
        SyncToSaved();
        if (Owner != null) Flash();
    }

    public void AutoLearnChordOnRest()
    {
        EnsureInitialized();
        if (Owner?.RunState == null) return;
        var pool = new List<string>();
        foreach (var cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
            pool.AddRange(ChordManager.GetLearnableChordIds(cat));
        var available = pool.Where(id => !_learnedChords.Contains(id) && !ChordManager.AllChords[id].IsTemporaryOnly)
            .ToList();
        if (available.Count == 0) return;
        var newChord = Owner.RunState.Rng.Niche.NextItem(available);
        _learnedChords.Add(newChord);
        SyncToSaved();
        Flash();
    }

    protected void FillCategorySlots(ChordCategory category)
    {
        EnsureInitialized();
        var targetCount = MaxLearnedChordsPerCategory;
        if (!_equippedChords.ContainsKey(category))
            _equippedChords[category] = new List<string>();
        var slots = _equippedChords[category];
        while (slots.Count < targetCount)
        {
            var available = ChordManager.GetLearnableChordIds(category)
                .Where(id => !_learnedChords.Contains(id) && !ChordManager.AllChords[id].IsTemporaryOnly)
                .ToList();
            if (available.Count == 0) break;
            var newChord = Owner.RunState.Rng.UpFront.NextItem(available);
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

        var rng = Owner.RunState.Rng.Niche;
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

    // ==================== 初始化与序列化 ====================
    protected void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;
        _equippedChords = new Dictionary<ChordCategory, List<string>>
        {
            { ChordCategory.Major, new List<string>() },
            { ChordCategory.Minor, new List<string>() },
            { ChordCategory.Dominant, new List<string>() }
        };
        _bonusChords = new List<string>();
        _temporaryChords = new List<string>();
        var hasAnyData = false;

        if (!string.IsNullOrEmpty(_savedChordsData))
        {
            foreach (var pair in _savedChordsData.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = pair.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out var catInt) &&
                    Enum.IsDefined(typeof(ChordCategory), catInt) && (ChordCategory)catInt != ChordCategory.Bonus)
                    _equippedChords[(ChordCategory)catInt].Add(parts[1]);
            }
            hasAnyData = true;
        }
        if (!string.IsNullOrEmpty(_savedBonusChordsData))
        {
            _bonusChords = _savedBonusChordsData.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            hasAnyData = true;
        }
        if (!string.IsNullOrEmpty(_savedTemporaryChordsData))
        {
            _temporaryChords = _savedTemporaryChordsData.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            hasAnyData = true;
        }
        if (!hasAnyData)
        {
            _equippedChords[ChordCategory.Major].Add("C");
            _equippedChords[ChordCategory.Minor].Add("Cm");
            _equippedChords[ChordCategory.Dominant].Add("C7");
        }
        if (!string.IsNullOrEmpty(_savedLearnedChordsData))
        {
            _learnedChords = _savedLearnedChordsData.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
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

    internal void SyncToSaved()
    {
        _savedChordsData = string.Join(";",
            _equippedChords.SelectMany(kv => kv.Value.Select(id => $"{(int)kv.Key}:{id}")));
        _savedBonusChordsData = string.Join(";", _bonusChords);
        _savedTemporaryChordsData = string.Join(";", _temporaryChords);
        _savedLearnedChordsData = string.Join(";", _learnedChords);
        if (Owner != null)
        {
            _pendingMigration[Owner] = (_savedChordsData, _savedBonusChordsData, _savedTemporaryChordsData);
            if (_bonusChords.Count > 0) _pendingBonusMigration[Owner] = new List<string>(_bonusChords);
        }
    }

    public void SetLearnedChordsFromString(string data)
    {
        EnsureInitialized();
        _learnedChords = data.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        SyncToSaved();
        if (Owner != null) Flash();
    }

    public void RestoreChordData(string chordsData, string bonusData, string tempData)
    {
        _savedChordsData = chordsData;
        _savedBonusChordsData = bonusData;
        _savedTemporaryChordsData = tempData;
        _initialized = false;
        EnsureInitialized();
        SyncToSaved();
        if (Owner != null) Flash();
    }

    public void CopyChordsTo(AnonGuitar target)
    {
        EnsureInitialized();
        foreach (var kv in _equippedChords)
            target._equippedChords[kv.Key] = new List<string>(kv.Value);
        target._bonusChords = new List<string>(_bonusChords);
        target._temporaryChords = new List<string>(_temporaryChords);
        target._learnedChords = new List<string>(_learnedChords);
        target.SyncToSaved();
        target.Flash();
    }

    // ==================== 兼容包装器 ====================
    public async Task PlayRandomEquippedChord(PlayerChoiceContext ctx, int count)
    {
        var chordIds = GetEquippedChordIds();
        if (chordIds.Count == 0) return;
        var rng = Owner.RunState.Rng.CombatCardSelection;
        for (int i = 0; i < count; i++)
            await ChordNoteSystem.PlayChordAsync(Owner, rng.NextItem(chordIds), ctx);
        ChordNoteUIManager.UpdateStoredChordDisplay(Owner);
    }

    public async Task PlaySpecificChord(PlayerChoiceContext ctx, string chordId, int count = 1)
    {
        for (int i = 0; i < count; i++)
            await ChordNoteSystem.PlayChordAsync(Owner, chordId, ctx);
        ChordNoteUIManager.UpdateStoredChordDisplay(Owner);
    }

    public async Task PlaySpecificChords(PlayerChoiceContext ctx, IReadOnlyList<string> chordIds, int countPerChord = 1)
    {
        foreach (var chordId in chordIds)
            for (int i = 0; i < countPerChord; i++)
                await ChordNoteSystem.PlayChordAsync(Owner, chordId, ctx);
        ChordNoteUIManager.UpdateStoredChordDisplay(Owner);
    }

    // ==================== Harmony 补丁 ====================
    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterRoomEntered))]
    private static class Hook_AfterRoomEntered_Patch
    {
        public static void Postfix(IRunState runState, AbstractRoom room)
        {
            if (runState?.Players == null) return;
            foreach (var player in runState.Players)
            {
                var guitar = player.Relics?.OfType<AnonGuitar>().FirstOrDefault();
                if (guitar != null)
                {
                    guitar._initialized = false;
                    guitar.EnsureInitialized();
                    guitar.SyncToSaved();
                }
            }
        }
    }
}