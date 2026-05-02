using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Events;
using CuteSakikoMod.CuteSakikoModCode.Nodes;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Systems;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;

[RegisterCharacterStarterRelic(typeof(CuteAnon))]
[RegisterTouchOfOrobasRefinement(typeof(FlashAnonGuitar))]
public class AnonGuitar : CuteAnonRelic
{
    protected static Dictionary<Player, List<string>> _pendingBonusTransfer = new();

    // ---------- 持久化字段 ----------
    [SavedProperty] private List<string> _savedChords = new();
    [SavedProperty] private List<string> _savedBonusChords = new();
    [SavedProperty] private List<string> _savedTemporaryChords = new();

    // ---------- 运行时数据 ----------
    private Dictionary<ChordCategory, string> _currentChords = new();
    private List<string> _bonusChords = new();
    private List<string> _temporaryChords = new();
    private bool _initialized;

    // UI
    private NoteDisplay _noteDisplay;
    private Dictionary<ChordCategory, string>? _preTempChords;
    private StoredChordDisplay _storedChordDisplay;

    public override RelicRarity Rarity => RelicRarity.Starter;
    protected virtual int MaxLearnedChordsPerCategory => 1;
    protected virtual int EffectMultiplier => 1;

    protected override IEnumerable<string> RegisteredKeywordIds => [CutesakiKeywords.RememberChord];

    // ========== 初始化 ==========
    private void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        _currentChords = new Dictionary<ChordCategory, string>();
        foreach (var raw in _savedChords)
        {
            var parts = raw.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out int catInt) &&
                Enum.IsDefined(typeof(ChordCategory), catInt))
            {
                _currentChords[(ChordCategory)catInt] = parts[1];
            }
        }

        _bonusChords = new List<string>(_savedBonusChords);
        _temporaryChords = new List<string>(_savedTemporaryChords);

        if (_currentChords.Count == 0)
        {
            _currentChords[ChordCategory.Major] = ChordManager.GetBaseChordId(ChordCategory.Major);
            _currentChords[ChordCategory.Minor] = ChordManager.GetBaseChordId(ChordCategory.Minor);
            _currentChords[ChordCategory.Dominant] = ChordManager.GetBaseChordId(ChordCategory.Dominant);
            SyncToSaved();
        }
    }

    private void SyncToSaved()
    {
        _savedChords = _currentChords.Select(kv => $"{(int)kv.Key}:{kv.Value}").ToList();
        _savedBonusChords = new List<string>(_bonusChords);
        _savedTemporaryChords = new List<string>(_temporaryChords);
    }

    public override async Task AfterObtained()
    {
        await base.AfterObtained();
        EnsureInitialized();
    }

    // ========== 公共接口 ==========
    public IReadOnlyList<string> GetTemporaryChords() => _temporaryChords.AsReadOnly();
    public int GetEffectMultiplier() => EffectMultiplier;
    public IReadOnlyList<string> GetBonusChords() => _bonusChords.AsReadOnly();

    public void AddBonusChord(string chordId)
    {
        EnsureInitialized();
        if (!ChordManager.AllChords.ContainsKey(chordId)) return;
        _bonusChords.Add(chordId);
        Flash();
        SyncToSaved();
    }

    public bool RemoveBonusChord(string chordId)
    {
        EnsureInitialized();
        if (_bonusChords.Remove(chordId)) { Flash(); SyncToSaved(); return true; }
        return false;
    }

    public bool HasBonusChord() => _bonusChords.Count > 0;
    public string GetBonusChord() => _bonusChords.FirstOrDefault();

    public void SetBonusChord(string chordId)
    {
        EnsureInitialized();
        if (!ChordManager.AllChords.ContainsKey(chordId)) return;
        _bonusChords.Clear();
        _bonusChords.Add(chordId);
        Flash();
        SyncToSaved();
    }

    public void TempReplaceChord(ChordCategory category, string tempChordId)
    {
        EnsureInitialized();
        if (!ChordManager.AllChords.TryGetValue(tempChordId, out var def) || !def.IsTemporaryOnly) return;
        if (!_currentChords.ContainsKey(category)) return;
        _preTempChords ??= new Dictionary<ChordCategory, string>();
        if (!_preTempChords.ContainsKey(category))
            _preTempChords[category] = _currentChords[category];
        _currentChords[category] = tempChordId;
        Flash();
        SyncToSaved();
    }

    public void RestoreTempChords()
    {
        EnsureInitialized();
        if (_preTempChords == null) return;
        foreach (var kv in _preTempChords)
            if (_currentChords.ContainsKey(kv.Key))
                _currentChords[kv.Key] = kv.Value;
        _preTempChords = null;
        Flash();
        SyncToSaved();
    }

    public void ReplaceChord(ChordCategory category, string newChordId)
    {
        EnsureInitialized();
        if (!ChordManager.AllChords.ContainsKey(newChordId)) return;
        _currentChords[category] = newChordId;
        Flash();
        SyncToSaved();
    }

    public void AddTemporaryChord(string chordId)
    {
        EnsureInitialized();
        if (!ChordManager.AllChords.ContainsKey(chordId)) return;
        _temporaryChords.Add(chordId);
        Flash();
        SyncToSaved();
    }

    public bool RemoveTemporaryChord(string chordId)
    {
        EnsureInitialized();
        if (_temporaryChords.Remove(chordId)) { Flash(); SyncToSaved(); return true; }
        return false;
    }

    public void ClearTemporaryChords()
    {
        EnsureInitialized();
        if (_temporaryChords.Count == 0) return;
        _temporaryChords.Clear();
        Flash();
        SyncToSaved();
    }

    public IReadOnlyDictionary<ChordCategory, string> GetCurrentChords() => _currentChords;

    public List<string> GetLearnedChordIds(params ChordCategory[] categories)
    {
        EnsureInitialized();
        var result = new List<string>();
        var filter = categories.Length > 0 ? new HashSet<ChordCategory>(categories) : null;
        foreach (var kv in _currentChords)
        {
            if (string.IsNullOrEmpty(kv.Value)) continue;
            if (kv.Key == ChordCategory.Bonus) continue;
            if (filter == null || filter.Contains(kv.Key)) result.Add(kv.Value);
        }
        if ((filter == null || filter.Contains(ChordCategory.Bonus)) && _bonusChords.Count > 0)
            result.AddRange(_bonusChords);
        if (_temporaryChords.Count > 0) result.AddRange(_temporaryChords);
        return result;
    }

    // ========== 统一演奏方法 ==========
    /// <summary>
    /// 演奏一个和弦（图标+效果+通知），可选择是否从储存区移除
    /// </summary>
    private async Task PlaySingleChord(PlayerChoiceContext ctx, string chordId, bool removeStored = true)
    {
        _ = ChordEffectPlayer.PlayChordIcons(Owner.Creature, new[] { chordId }, 0f);
        if (ChordManager.AllChords.TryGetValue(chordId, out var def))
            await def.Effect(ctx, Owner.Creature, EffectMultiplier);
        if (removeStored)
            MusicNoteManager.RemoveChord(Owner, chordId);
        await NotifyChordPlayed(ctx);
    }

    // ========== 核心方法：音符处理 ==========
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        EnsureInitialized();
        if (cardPlay.Card.Owner != Owner) return;
        if (Owner.Creature.CombatState == null) return;

        var noNoteKeyword = CutesakiKeywords.NoNote;
        if (noNoteKeyword != null && cardPlay.Card.HasModKeyword(noNoteKeyword))
        {
            UpdateNoteDisplay();
            UpdateStoredChordDisplay();
            return;
        }

        var storedBefore = MusicNoteManager.GetStoredChords(Owner);
        string? overflowChordId = null;
        var hasLingering = Owner.Creature.HasPower<LingeringTastePower>();
        if (hasLingering && storedBefore.Count >= MusicNoteManager.MaxStoredChords && storedBefore.Count > 0)
            overflowChordId = storedBefore[0];

        var (newChords, actualOverflow) = MusicNoteManager.AddNote(
            Owner, cardPlay.Card.Type, _currentChords, _bonusChords.Concat(_temporaryChords));

        // 溢出（意犹未尽）：如果 AddNote 实际溢出了一个和弦，而又与我们的预测相符，则演奏
        if (newChords.Count > 0 && overflowChordId != null)
        {
            // actualOverflow 可能为 null 如果没发生溢出（比如音符未构成新和弦）
            if (actualOverflow == overflowChordId)
                await PlaySingleChord(choiceContext, overflowChordId, removeStored: false);
        }

        var hasAutoPlay = Owner.Creature.HasPower<PlayImmediatelyPower>();
        if (hasAutoPlay && newChords.Count > 0)
            foreach (var chordId in newChords)
                await PlaySingleChord(choiceContext, chordId, removeStored: false);

        if (newChords.Count == 0)
            foreach (var power in Owner.Creature.Powers.OfType<StageNervesPower>())
                await power.OnNoteWithoutChord();

        UpdateNoteDisplay();
        UpdateStoredChordDisplay();
    }

    /// <summary>
    /// 当产生一个新音符时调用（打牌或扫弦等），统一处理溢出和自动播放
    /// </summary>
    public async Task OnNoteGenerated(PlayerChoiceContext choiceContext, CardType noteType)
    {
        if (Owner.Creature.CombatState == null) return;

        // 添加音符，同时获得新和弦列表和溢出的旧和弦ID
        var (newChords, overflowChordId) = MusicNoteManager.AddNote(
            Owner, noteType, _currentChords, _bonusChords.Concat(_temporaryChords));

        // 溢出演奏（如果有）
        if (newChords.Count > 0 && overflowChordId != null)
            await PlaySingleChord(choiceContext, overflowChordId, removeStored: false); // 关键：不移除储存区

        // 自动演奏（即刻演奏）
        var hasAutoPlay = Owner.Creature.HasPower<PlayImmediatelyPower>();
        if (hasAutoPlay && newChords.Count > 0)
            foreach (var chordId in newChords)
                await PlaySingleChord(choiceContext, chordId, removeStored: false);

        if (newChords.Count == 0)
            foreach (var power in Owner.Creature.Powers.OfType<StageNervesPower>())
                await power.OnNoteWithoutChord();

        UpdateNoteDisplay();
        UpdateStoredChordDisplay();
    }

    // ========== 其他演奏触发方法 ==========
    public void ClearSequence()
    {
        MusicNoteManager.ClearNotes(Owner);
        MusicNoteManager.ClearStoredChords(Owner);
        UpdateNoteDisplay();
        UpdateStoredChordDisplay();
    }

    public async Task TriggerAllStoredChords(PlayerChoiceContext choiceContext)
    {
        var stored = MusicNoteManager.GetStoredChords(Owner).ToList();
        foreach (var chordId in stored)
            await PlaySingleChord(choiceContext, chordId, removeStored: true);
        ClearSequence();
    }

    public async Task TriggerAllStoredChordsKeepNotes(PlayerChoiceContext choiceContext)
    {
        var stored = MusicNoteManager.GetStoredChords(Owner).ToList();
        foreach (var chordId in stored)
            await PlaySingleChord(choiceContext, chordId, removeStored: false);
        MusicNoteManager.ClearStoredChords(Owner);
        UpdateStoredChordDisplay();
        SyncToSaved();
    }

    public async Task AddChordToStored(PlayerChoiceContext choiceContext, string chordId)
    {
        if (!ChordManager.AllChords.ContainsKey(chordId)) return;
        MusicNoteManager.AddChordDirectly(Owner, chordId);
        var hasAutoPlay = Owner.Creature.HasPower<PlayImmediatelyPower>();
        if (hasAutoPlay)
            await PlaySingleChord(choiceContext, chordId, removeStored: false);
        UpdateStoredChordDisplay();
        SyncToSaved();
    }

    public async Task TriggerLearnedChords(PlayerChoiceContext choiceContext, params ChordCategory[] categories)
    {
        var chordIds = GetLearnedChordIds(categories);
        foreach (var chordId in chordIds)
            await PlaySingleChord(choiceContext, chordId, removeStored: false);
    }

    public async Task TriggerAllLearnedChords(PlayerChoiceContext choiceContext)
        => await TriggerLearnedChords(choiceContext);

    public async Task TriggerLastStoredChord(PlayerChoiceContext choiceContext)
    {
        var stored = MusicNoteManager.GetStoredChords(Owner);
        if (stored.Count == 0) return;
        await PlaySingleChord(choiceContext, stored.Last(), removeStored: true);
        UpdateStoredChordDisplay();
        SyncToSaved();
    }

    // ========== 通知（公开） ==========
    public async Task NotifyChordPlayed(PlayerChoiceContext choiceContext)
    {
        foreach (var power in Owner.Creature.Powers.OfType<UnforgettablePerformancePower>())
            await power.OnChordPlayed(choiceContext);

        const string curtainCallId = "CUTESAKIKOMOD-CURTAIN_CALL";
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

    // ========== 悬浮提示 ==========
    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            if (!IsMutable) yield break;
            var desc = new LocString("relics", "ANON_GUITAR_CHORDS_DESC");
            var lines = new List<string>();

            foreach (var kv in _currentChords.Where(kv => kv.Key != ChordCategory.Bonus))
                if (ChordManager.AllChords.TryGetValue(kv.Value, out var def))
                {
                    var title = new LocString("card_keywords", def.TitleKey).GetFormattedText();
                    var text = ChordDisplayHelper.GetFormattedDescription(def, EffectMultiplier);
                    lines.Add($"[{title}]({def.GetConditionText()})\n{text}");
                }

            foreach (var chordId in _bonusChords)
                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                {
                    var title = new LocString("card_keywords", def.TitleKey).GetFormattedText();
                    var text = ChordDisplayHelper.GetFormattedDescription(def, EffectMultiplier);
                    lines.Add($"[{title}]({def.GetConditionText()})\n{text}");
                }

            foreach (var chordId in _temporaryChords)
                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                {
                    var title = new LocString("card_keywords", def.TitleKey).GetFormattedText();
                    var text = ChordDisplayHelper.GetFormattedDescription(def, EffectMultiplier);
                    lines.Add($"[临时] [{title}]({def.GetConditionText()})\n{text}");
                }

            desc.Add("Chords", string.Join("\n\n", lines));
            yield return new HoverTip(new LocString("relics", "ANON_GUITAR_CHORDS_TITLE"), desc);
        }
    }

    // ========== UI 管理 ==========
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
        else if (Owner?.Creature?.CombatState != null)
            EnsureNoteDisplay();
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
            _storedChordDisplay.UpdateChords(stored, EffectMultiplier);
        }
        else if (Owner?.Creature?.CombatState != null)
        {
            EnsureStoredChordDisplay();
        }
    }

    private void CleanupUI()
    {
        if (_noteDisplay != null && GodotObject.IsInstanceValid(_noteDisplay))
            _noteDisplay.QueueFree();
        _noteDisplay = null;
        if (_storedChordDisplay != null && GodotObject.IsInstanceValid(_storedChordDisplay))
            _storedChordDisplay.QueueFree();
        _storedChordDisplay = null;
    }

    // ========== 休息站选项 ==========
    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;
        var canLearn = ChordManager.GetLearnableChordIds(ChordCategory.Major).Count > 0 ||
                       ChordManager.GetLearnableChordIds(ChordCategory.Minor).Count > 0 ||
                       ChordManager.GetLearnableChordIds(ChordCategory.Dominant).Count > 0;
        if (!canLearn) return false;
        options.Add(new PracticeGuitarOption(player, this));
        return true;
    }

    // ========== 遗物移除/战斗结束 ==========
    public override async Task AfterRemoved()
    {
        if (_bonusChords.Count > 0)
            _pendingBonusTransfer[Owner] = new List<string>(_bonusChords);

        CleanupUI();
        MusicNoteManager.ClearAll(Owner);
        await base.AfterRemoved();
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        RestoreTempChords();
        _temporaryChords.Clear();
        MusicNoteManager.ClearCombatData(Owner);
        CleanupUI();
        SyncToSaved();
        await base.AfterCombatEnd(room);
    }

    public void CopyChordsTo(AnonGuitar target)
    {
        EnsureInitialized();
        foreach (var kv in _currentChords) target._currentChords[kv.Key] = kv.Value;
        target._bonusChords = new List<string>(_bonusChords);
        target.SyncToSaved();
    }
}