using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;
using CuteSakikoMod.CuteSakikoModCode.Character.Mygo;
using CuteSakikoMod.CuteSakikoModCode.Nodes;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;

[RegisterCharacterStarterRelic(typeof(CuteAnon))]
[RegisterTouchOfOrobasRefinement(typeof(FlashAnonGuitar))]
public class AnonGuitar : CuteAnonRelic, IModRightClickableRelic
{
    protected static Dictionary<Player, (string chords, string bonus, string temp)> _pendingMigration = new();
    protected static Dictionary<Player, List<string>> _pendingBonusMigration = new();

    private static readonly string AudioDir =
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "audio");

    private static readonly string[] StrumFiles =
        { "guitar_strum1.mp3", "guitar_strum2.mp3", "guitar_strum3.mp3", "guitar_strum4.mp3", "guitar_strum5.mp3" };

    private static readonly Random _rand = new();
    protected List<string> _bonusChords = new(); // 额外槽位（不限种类）
    private bool _chordBonusConsumedThisOperation;

    private bool _curtainCallRecalledThisTurn;
    
    /// <summary> 用于 UI 显示的加成值：战斗内包含所有加成，战斗外仅基础值 </summary>
    public int GetDisplayBonus()
    {
        return Owner?.Creature?.CombatState != null ? GetTotalBonus() : BaseChordBonus;
    }

    // 装备和弦：每个分类一个列表（支持多槽位）
    protected Dictionary<ChordCategory, List<string>> _equippedChords = new();
    private bool _firstPlayBonusAppliedThisOperation;
    private bool _firstPlayBonusConsumedThisTurn;
    protected bool _initialized;

    protected List<string> _learnedChords = new();

    private NoteDisplay _noteDisplay;
    protected string _savedBonusChordsData = "";

    // 序列化字段
    protected string _savedChordsData = "";
    protected string _savedLearnedChordsData = "";
    protected string _savedTemporaryChordsData = "";
    private StoredChordDisplay _storedChordDisplay;

    protected List<string> _temporaryChords = new(); // 临时槽位

    // 新增字段（放在 _chordBonusConsumedThisOperation 附近）
    // 新增虚拟属性（放在 BaseChordBonus 附近）
    protected virtual int FirstPlayBonus => 1;

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

    public override RelicRarity Rarity => RelicRarity.Starter;
    protected virtual int MaxLearnedChordsPerCategory => 1;
    protected virtual int BaseChordBonus => 0;

    protected override IEnumerable<string> RegisteredKeywordIds => [CutesakiKeywords.RememberChord];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (!IsMutable) yield break;
            var desc = new LocString("relics", "CUTE_SAKIKO_MOD_RELIC_ANON_GUITAR_CHORDS_DESC");
            var lines = new List<string>();
            foreach (var cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
            foreach (var chordId in _equippedChords.GetValueOrDefault(cat, new List<string>()))
                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                {
                    var title = new LocString("card_keywords", def.TitleKey).GetFormattedText();
                    var text = ChordDisplayHelper.GetFormattedDescription(def, GetDisplayBonus());
                    var condition = ChordSequenceModifierHelper.GetModifiedConditionText(def, Owner.Creature);
                    lines.Add($"[{title}]({condition})\n{text}");
                }

            foreach (var chordId in _bonusChords)
                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                {
                    var title = new LocString("card_keywords", def.TitleKey).GetFormattedText();
                    var text = ChordDisplayHelper.GetFormattedDescription(def,GetDisplayBonus());
                    var condition = ChordSequenceModifierHelper.GetModifiedConditionText(def, Owner.Creature);
                    lines.Add($"[{title}]({condition})\n{text}");
                }

            foreach (var chordId in _temporaryChords)
                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                {
                    var title = new LocString("card_keywords", def.TitleKey).GetFormattedText();
                    var text = ChordDisplayHelper.GetFormattedDescription(def, GetDisplayBonus());
                    var condition = ChordSequenceModifierHelper.GetModifiedConditionText(def, Owner.Creature);
                    lines.Add($"[临时] [{title}]({condition})\n{text}");
                }

            desc.Add("Chords", string.Join("\n\n", lines));
            yield return new HoverTip(new LocString("relics", "CUTE_SAKIKO_MOD_RELIC_ANON_GUITAR_CHORDS_TITLE"), desc);
        }
    }

    public bool CanHandleRightClickLocal(ModRightClickContext context)
    {
        return true;
    }

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

    /// <summary> 公开获取每类别最大槽位数 </summary>
    public int GetMaxChordsPerCategory()
    {
        return MaxLearnedChordsPerCategory;
    }

    /// <summary> 为指定类别填充空槽位至最大数量（随机学习并装备） </summary>
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
            AddEquippedChord(category, newChord); // 内部会调用 AddToLearnedIfMissing 并 SyncToSaved
        }
    }

    public void SetLearnedChordsFromString(string data)
    {
        EnsureInitialized();
        _learnedChords = data.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
        SyncToSaved();
        if (Owner != null) Flash();
    }

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

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        EnsureInitialized();
    }

    // 获取所有用于匹配的和弦ID列表（包括主槽位、额外、临时）
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

    // 获取指定分类的所有装备和弦（含额外和临时）——带参数分类筛选
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

    // 仅获取指定分类的装备槽位列表（不含额外、临时）
    public IReadOnlyList<string> GetCategorySlots(ChordCategory category)
    {
        EnsureInitialized();
        return _equippedChords.TryGetValue(category, out var list)
            ? list.AsReadOnly()
            : new List<string>().AsReadOnly();
    }

    // 已学习和弦
    public IReadOnlyList<string> GetLearnedChords()
    {
        EnsureInitialized();
        return _learnedChords.AsReadOnly();
    }

    // 类别槽位操作
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
        if (_equippedChords[category].Remove(chordId))
        {
            SyncToSaved();
            if (Owner != null) Flash();
            return true;
        }

        return false;
    }

    // 额外槽位
    public IReadOnlyList<string> GetBonusChords()
    {
        EnsureInitialized();
        return _bonusChords.AsReadOnly();
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
        if (_bonusChords.Remove(chordId))
        {
            if (Owner != null) Flash();
            SyncToSaved();
            return true;
        }

        return false;
    }

    // 临时槽位
    public IReadOnlyList<string> GetTemporaryChords()
    {
        EnsureInitialized();
        return _temporaryChords.AsReadOnly();
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
        if (_temporaryChords.Remove(chordId))
        {
            if (Owner != null) Flash();
            SyncToSaved();
            return true;
        }

        return false;
    }

    public void ClearTemporaryChords()
    {
        EnsureInitialized();
        if (_temporaryChords.Count == 0) return;
        _temporaryChords.Clear();
        if (Owner != null) Flash();
        SyncToSaved();
    }

    private void AddToLearnedIfMissing(string chordId)
    {
        if (!_learnedChords.Contains(chordId))
            _learnedChords.Add(chordId);
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

    public int GetTotalBonus()
    {
        var bonus = BaseChordBonus;
        if (Owner?.Creature != null)
        {
            foreach (var provider in Owner.Creature.Powers.OfType<IChordBonusProvider>())
                bonus += provider.GetBonus();
            foreach (var provider in Owner.Relics.OfType<IChordBonusProvider>())
                bonus += provider.GetBonus();

            var chordBonusPower = Owner.Creature.GetPower<ChordBonusPower>();
            if (chordBonusPower != null && chordBonusPower.Amount > 0)
                bonus += 1; // 每次演奏 +1，不再使用层数作为加数值

            if ((!_firstPlayBonusConsumedThisTurn && FirstPlayBonus > 0) || _firstPlayBonusAppliedThisOperation)
                bonus += FirstPlayBonus;
        }
        return bonus;
    }

    private void TryConsumeFirstPlayBonus()
    {
        if (!_firstPlayBonusConsumedThisTurn && FirstPlayBonus > 0)
        {
            _firstPlayBonusConsumedThisTurn = true;
            _firstPlayBonusAppliedThisOperation = true;
        }
    }
    
    /// <summary>
    /// 随机演奏已记忆和弦指定次数，会消耗回合首次演奏加成和 ChordBonusPower。
    /// </summary>
    public async Task PlayRandomEquippedChord(PlayerChoiceContext ctx, int count)
    {
        _firstPlayBonusAppliedThisOperation = false;
        _chordBonusConsumedThisOperation = false;
        TryConsumeFirstPlayBonus();

        var chordIds = GetEquippedChordIds();
        if (chordIds.Count == 0) return;

        var rng = Owner.RunState.Rng.CombatCardSelection;
        for (int i = 0; i < count; i++)
        {
            var randomChordId = rng.NextItem(chordIds);
            await PlaySingleChord(ctx, randomChordId, 1, false);
        }
        UpdateStoredChordDisplay();
    }
    
    
    /// <summary>
    /// 立即演奏一个随机已装备和弦，会消耗回合首次演奏加成和 ChordBonusPower。
    /// </summary>
    public async Task PlayRandomEquippedChordImmediate()
    {
        _firstPlayBonusAppliedThisOperation = false;
        _chordBonusConsumedThisOperation = false;
        TryConsumeFirstPlayBonus();

        var chordIds = GetAllEquippedChords();
        if (chordIds.Count == 0) return;
        var rng = Owner.RunState.Rng.CombatCardSelection;
        var randomChord = rng.NextItem(chordIds);

        await PlaySingleChord(null, randomChord, 1, false); // 使用 null 上下文，和原 Recorder 一致
        UpdateStoredChordDisplay();
    }
    
    /// <summary>
    /// 演奏单个指定和弦（用于已选定的和弦），会消耗回合首次演奏加成和 ChordBonusPower。
    /// 注意：如果需要演奏多个指定和弦并共享一次加成，请使用 PlaySpecificChords。
    /// </summary>
    public async Task PlaySpecificChord(PlayerChoiceContext ctx, string chordId, int count = 1)
    {
        _firstPlayBonusAppliedThisOperation = false;
        _chordBonusConsumedThisOperation = false;
        TryConsumeFirstPlayBonus();

        await PlaySingleChord(ctx, chordId, count, false);
        UpdateStoredChordDisplay();
    }

    /// <summary>
    /// 一次性演奏多个指定和弦，整个操作共享一次回合首次加成和 ChordBonusPower 消耗。
    /// </summary>
    /// <summary>
    /// 一次性演奏多个指定和弦，整个操作共享一次回合首次加成和 ChordBonusPower 消耗。
    /// </summary>
    public async Task PlaySpecificChords(PlayerChoiceContext ctx, IReadOnlyList<string> chordIds, int countPerChord = 1)
    {
        _firstPlayBonusAppliedThisOperation = false;
        _chordBonusConsumedThisOperation = false;
        TryConsumeFirstPlayBonus();

        foreach (var chordId in chordIds)
            await PlaySingleChord(ctx, chordId, countPerChord, false);

        UpdateStoredChordDisplay();
    }

    public async Task AutoPlayNewChords(PlayerChoiceContext ctx, MusicNoteManager.NoteProcessResult result)
    {
        // 清理操作标志（不在这里消耗首次加成）
        _firstPlayBonusAppliedThisOperation = false;
        _chordBonusConsumedThisOperation = false;

        if (Owner.Creature == null) return;

        // 溢出且立即演奏（需要 LingeringTastePower）
        if (result.OverflowChord != null && Owner.Creature.HasPower<LingeringTastePower>())
        {
            TryConsumeFirstPlayBonus();
            await PlaySingleChord(ctx, result.OverflowChord, removeStored: false);
            UpdateStoredChordDisplay();
            return; // 已演奏，直接返回
        }

        // 拥有 PlayImmediatelyPower 且存在新和弦 → 立即演奏
        var playImmediately = Owner.Creature.GetPower<PlayImmediatelyPower>();
        if (playImmediately != null && playImmediately.Amount > 0 && result.NewChords.Count > 0)
        {
            TryConsumeFirstPlayBonus();
            var chordsToPlay = result.NewChords.ToList();
            foreach (var chordId in chordsToPlay)
            {
                if (playImmediately.Amount <= 0) break;
                await PlaySingleChord(ctx, chordId, removeStored: false);
                MusicNoteManager.RemoveChord(Owner, chordId);
                await PowerCmd.Decrement(playImmediately);
            }

            UpdateStoredChordDisplay();
        }
        else if (result.NewChords.Count == 0)
        {
            // 无新和弦 → StageNerves，不演奏也不消耗加成
            foreach (var power in Owner.Creature.Powers.OfType<StageNervesPower>())
                await power.OnNoteWithoutChord();
        }
        // 如果新和弦只是被储存而没有立即演奏，不消耗加成，也不做任何操作
    }

    public async Task TriggerAllStoredChords(PlayerChoiceContext choiceContext, int count = 1)
    {
        _firstPlayBonusAppliedThisOperation = false; // 确保干净状态
        _chordBonusConsumedThisOperation = false;
        TryConsumeFirstPlayBonus();
        var stored = MusicNoteManager.GetStoredChords(Owner).ToList();
        foreach (var chordId in stored)
            await PlaySingleChord(choiceContext, chordId, count);
        ClearSequence();
    }

    public async Task TriggerAllStoredChordsKeepNotes(PlayerChoiceContext choiceContext, int count = 1)
    {
        _firstPlayBonusAppliedThisOperation = false; // 确保干净状态
        _chordBonusConsumedThisOperation = false;
        TryConsumeFirstPlayBonus();
        var stored = MusicNoteManager.GetStoredChords(Owner).ToList();
        foreach (var chordId in stored)
            await PlaySingleChord(choiceContext, chordId, count, false);
        MusicNoteManager.ClearStoredChords(Owner);
        UpdateStoredChordDisplay();
        SyncToSaved();
    }

    public async Task AddChordToStored(PlayerChoiceContext choiceContext, string chordId, int count = 1)
    {
        _firstPlayBonusAppliedThisOperation = false; // 确保干净状态
        _chordBonusConsumedThisOperation = false;
        // 注意：不在此处消耗首次加成，因为可能不会演奏

        if (!ChordManager.AllChords.ContainsKey(chordId)) return;

        var playImmediately = Owner.Creature.GetPower<PlayImmediatelyPower>();
        if (playImmediately != null && playImmediately.Amount > 0)
        {
            TryConsumeFirstPlayBonus(); // 立即演奏，消耗加成
            await PlaySingleChord(choiceContext, chordId, count, false);
            await PowerCmd.Decrement(playImmediately);
            UpdateStoredChordDisplay(); // 补充 UI 刷新
            return;
        }

        var hasLingering = Owner.Creature.HasPower<LingeringTastePower>();

        for (var i = 0; i < count; i++)
        {
            var storedBefore = MusicNoteManager.GetStoredChords(Owner);
            MusicNoteManager.AddChordDirectly(Owner, chordId);

            if (hasLingering && storedBefore.Count >= MusicNoteManager.MaxStoredChords)
            {
                TryConsumeFirstPlayBonus(); // 溢出演奏，消耗加成（仅首次溢出时）
                await PlaySingleChord(choiceContext, storedBefore[0], 1, false);
            }
        }

        UpdateStoredChordDisplay();
        SyncToSaved();
    }

    public async Task TriggerAllEquippedChords(PlayerChoiceContext choiceContext, int count = 1)
    {
        _firstPlayBonusAppliedThisOperation = false; // 确保干净状态
        _chordBonusConsumedThisOperation = false;
        TryConsumeFirstPlayBonus();
        await TriggerEquippedChords(choiceContext, count);
    }

    public async Task TriggerLastStoredChord(PlayerChoiceContext choiceContext, int count = 1)
    {
        _firstPlayBonusAppliedThisOperation = false; // 确保干净状态
        _chordBonusConsumedThisOperation = false;
        TryConsumeFirstPlayBonus();
        var stored = MusicNoteManager.GetStoredChords(Owner);
        if (stored.Count == 0) return;
        await PlaySingleChord(choiceContext, stored.Last(), count);
        UpdateStoredChordDisplay();
        SyncToSaved();
    }

    public async Task TriggerLearnedChords(PlayerChoiceContext choiceContext, int count = 1,
        params ChordCategory[] categories)
    {
        var chordIds = GetLearnedChordIds(categories);
        GD.Print($"[TriggerLearnedChords] 将要演奏 {chordIds.Count} 个已学习和弦");
        for (int idx = 0; idx < chordIds.Count; idx++)
        {
            var chordId = chordIds[idx];
            GD.Print($"[TriggerLearnedChords] 演奏第 {idx+1}/{chordIds.Count} 个和弦: {chordId}");
            await PlaySingleChord(choiceContext, chordId, count, false);
        }
        GD.Print("[TriggerLearnedChords] 所有已学习和弦演奏结束");
    }

    public async Task TriggerEquippedChords(PlayerChoiceContext choiceContext, int count = 1,
        params ChordCategory[] categories)
    {
        var chordIds = GetEquippedChordIds(categories);
        GD.Print($"[TriggerEquippedChords] 将要演奏 {chordIds.Count} 个已记忆和弦");
        for (int idx = 0; idx < chordIds.Count; idx++)
        {
            var chordId = chordIds[idx];
            GD.Print($"[TriggerEquippedChords] 演奏第 {idx+1}/{chordIds.Count} 个和弦: {chordId}");
            await PlaySingleChord(choiceContext, chordId, count, false);
        }
        GD.Print("[TriggerEquippedChords] 所有已记忆和弦演奏结束");
    }

    public async Task TriggerAllLearnedChords(PlayerChoiceContext choiceContext, int count = 1)
    {
        _firstPlayBonusAppliedThisOperation = false;
        _chordBonusConsumedThisOperation = false;
        TryConsumeFirstPlayBonus();
        await TriggerLearnedChords(choiceContext, count);
        UpdateStoredChordDisplay(); // 确保 UI 反映演奏后的状态
    }

    private async Task PlaySingleChord(PlayerChoiceContext ctx, string chordId, int count = 1, bool removeStored = true)
    {
        var chordBonusPower = Owner.Creature?.GetPower<ChordBonusPower>();
        bool shouldConsumeChordBonus =
            chordBonusPower != null && chordBonusPower.Amount > 0 && !_chordBonusConsumedThisOperation;
        if (shouldConsumeChordBonus)
            _chordBonusConsumedThisOperation = true;

        // ★ 在循环开始前固定本次操作的首次演奏加成
        int fixedFirstPlayBonus = _firstPlayBonusAppliedThisOperation ? FirstPlayBonus : 0;

        for (var i = 0; i < count; i++)
        {
            _ = ChordEffectPlayer.PlayChordIcons(Owner.Creature, new[] { chordId }, 0f);
            if (ChordManager.AllChords.TryGetValue(chordId, out var def))
            {
                // 使用固定加成而非实时读取
                int baseBonus = GetTotalBonus() - (_firstPlayBonusAppliedThisOperation ? FirstPlayBonus : 0);
                int totalBonus = baseBonus + fixedFirstPlayBonus;
                await def.Effect(ctx, Owner.Creature, totalBonus);
            }

            if (removeStored)
                MusicNoteManager.RemoveChord(Owner, chordId);
            await NotifyChordPlayed(ctx);
            var sfx = Path.Combine(AudioDir, StrumFiles[_rand.Next(StrumFiles.Length)]);
            AudioManager.PlaySound(sfx);
        }

        if (shouldConsumeChordBonus && chordBonusPower != null)
        {
            await PowerCmd.Decrement(chordBonusPower);
            UpdateStoredChordDisplay();
        }
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        EnsureInitialized();
        if (cardPlay.Card.Owner != Owner) return;
        if (Owner.Creature.CombatState == null) return;
        if (CutesakiKeywords.NoNote != null &&
            cardPlay.Card.Keywords.Contains(CutesakiKeywords.NoNote.GetModCardKeyword()))
        {
            UpdateNoteDisplay();
            UpdateStoredChordDisplay();
            return;
        }

        await MusicNoteManager.AddNoteAndAutoPlayAsync(
            Owner, cardPlay.Card.Type, GetAllEquippedChords(), choiceContext);

        await HandleMessyPlay(choiceContext);
        UpdateNoteDisplay();
        UpdateStoredChordDisplay();
    }

    private async Task HandleMessyPlay(PlayerChoiceContext choiceContext)
    {
        var messyPlay = Owner.Creature?.GetPower<MessyPlayPower>();
        if (messyPlay == null || messyPlay.Amount <= 0) return;
        if (messyPlay.OnNoteObtained())
        {
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
    }

    public async Task OnNoteGenerated(PlayerChoiceContext choiceContext, CardType noteType)
    {
        if (Owner.Creature.CombatState == null) return;
        await MusicNoteManager.AddNoteAndAutoPlayAsync(
            Owner, noteType, GetAllEquippedChords(), choiceContext);

        await HandleMessyPlay(choiceContext);
        UpdateNoteDisplay();
        UpdateStoredChordDisplay();
    }

    public void ClearSequence()
    {
        MusicNoteManager.ClearNotes(Owner);
        MusicNoteManager.ClearStoredChords(Owner);
        UpdateNoteDisplay();
        UpdateStoredChordDisplay();
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        Entry.Logger.Debug($"[AfterPlayerTurnStart] Player {player.NetId} entering");
        if (player != Owner) return;
        _curtainCallRecalledThisTurn = false;
        _firstPlayBonusConsumedThisTurn = false;
        _firstPlayBonusAppliedThisOperation = false;
        UpdateStoredChordDisplay(); // 刷新 UI 以显示潜在的首次加成
        await Task.CompletedTask;
    }

    public async Task NotifyChordPlayed(PlayerChoiceContext choiceContext)
    {
        foreach (var power in Owner.Creature.Powers.OfType<UnforgettablePerformancePower>())
            if (power.OnChordPlayed() && power.Amount > 0)
                await PlayerCmd.GainEnergy(power.Amount, Owner);
        if (_curtainCallRecalledThisTurn) return;
        _curtainCallRecalledThisTurn = true;
        var curtainCallId = ModelDb.Card<CurtainCall>().Id.Entry;
        var player = Owner;
        if (player == null) return;
        var cardsToMove = new List<CardModel>();
        var searchPiles = new[] { PileType.Discard, PileType.Draw, PileType.Exhaust };
        foreach (var pileType in searchPiles)
        {
            var pile = pileType.GetPile(player);
            if (pile == null) continue;
            cardsToMove.AddRange(pile.Cards.Where(c => c.Id.Entry == curtainCallId));
        }

        foreach (var card in cardsToMove)
            await CardPileCmd.Add(card, PileType.Hand);
        if (cardsToMove.Count > 0) Flash();
    }

    public List<string> GetLearnedChordIds(params ChordCategory[] categories)
    {
        EnsureInitialized();
        if (categories.Length == 0)
            return new List<string>(_learnedChords);

        var filter = new HashSet<ChordCategory>(categories);
        return _learnedChords
            .Where(id => ChordManager.AllChords.TryGetValue(id, out var def) && filter.Contains(def.Category))
            .ToList();
    }

    private void EnsureNoteDisplay()
    {
        if (_noteDisplay != null && GodotObject.IsInstanceValid(_noteDisplay)) return;
        if (Owner?.Creature?.CombatState == null) return;
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner.Creature);
        if (creatureNode == null) return;
        var scene = GD.Load<PackedScene>("res://CuteSakikoMod/scenes/ui/note_display.tscn");
        if (scene == null) return;
        var display = scene.Instantiate<Control>();
        creatureNode.AddChild(display);
        display.Position = new Vector2(-110, -383);
        _noteDisplay = display as NoteDisplay;
        _noteDisplay?.UpdateNotes(MusicNoteManager.GetCurrentNotes(Owner));
    }

    public void UpdateNoteDisplay()
    {
        if (_noteDisplay != null && GodotObject.IsInstanceValid(_noteDisplay))
            _noteDisplay.UpdateNotes(MusicNoteManager.GetCurrentNotes(Owner));
        else if (Owner?.Creature?.CombatState != null) EnsureNoteDisplay();
    }

    private void EnsureStoredChordDisplay()
    {
        if (_storedChordDisplay != null && GodotObject.IsInstanceValid(_storedChordDisplay)) return;
        if (Owner?.Creature?.CombatState == null) return;
        var creatureNode = NCombatRoom.Instance?.GetCreatureNode(Owner.Creature);
        if (creatureNode == null) return;
        var scene = GD.Load<PackedScene>("res://CuteSakikoMod/scenes/ui/stored_chord_display.tscn");
        if (scene == null) return;
        var display = scene.Instantiate<Control>();
        creatureNode.AddChild(display);
        display.Position = new Vector2(-140, -230);
        _storedChordDisplay = display as StoredChordDisplay;
        UpdateStoredChordDisplay();
    }

    public void UpdateStoredChordDisplay()
    {
        if (_storedChordDisplay != null && GodotObject.IsInstanceValid(_storedChordDisplay))
        {
            var stored = MusicNoteManager.GetStoredChords(Owner).ToList();
            _storedChordDisplay.UpdateChords(stored, GetDisplayBonus());
        }
        else if (Owner?.Creature?.CombatState != null)
        {
            EnsureStoredChordDisplay();
        }
    }

    private void CleanupUI()
    {
        if (_noteDisplay != null && GodotObject.IsInstanceValid(_noteDisplay)) _noteDisplay.QueueFree();
        _noteDisplay = null;
        if (_storedChordDisplay != null && GodotObject.IsInstanceValid(_storedChordDisplay))
            _storedChordDisplay.QueueFree();
        _storedChordDisplay = null;
    }

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        await base.AfterRoomEntered(room);
        if (room is RestSiteRoom) AutoLearnChordOnRest();
    }

    public override async Task AfterRemoved()
    {
        SyncToSaved();
        if (Owner != null)
            _pendingMigration[Owner] = (_savedChordsData, _savedBonusChordsData, _savedTemporaryChordsData);
        CleanupUI();
        MusicNoteManager.ClearAll(Owner);
        await base.AfterRemoved();
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        ClearTemporaryChords();
        MusicNoteManager.ClearCombatData(Owner);
        ChordSequenceModifierHelper.ClearCardModifiers(Owner);
        CleanupUI();
        SyncToSaved();
        await base.AfterCombatEnd(room);
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