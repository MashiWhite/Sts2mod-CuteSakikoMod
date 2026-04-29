using CuteSakikoMod.CuteSakikoModCode.Character;
using CuteSakikoMod.CuteSakikoModCode.Events;
using CuteSakikoMod.CuteSakikoModCode.Nodes;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
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

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;

[RegisterCharacterStarterRelic(typeof(CuteAnon))]
[RegisterTouchOfOrobasRefinement(typeof(FlashAnonGuitar))]
public class AnonGuitar : CuteAnonRelic
{
    protected static Dictionary<Player, List<string>> _pendingBonusTransfer = new();

    // 临时已记忆和弦列表（用于练习的证明等效果）
    private readonly List<string> _temporaryChords = new();

    [SavedProperty] private List<string> _bonusChords = new();


    [SavedProperty] private Dictionary<ChordCategory, string> _currentChords = new()
    {
        [ChordCategory.Major] = "C",
        [ChordCategory.Minor] = "Am",
        [ChordCategory.Dominant] = "G7"
    };

    [SavedProperty] private bool _initialized;

    private NoteDisplay _noteDisplay;
    private Dictionary<ChordCategory, string>? _preTempChords;
    private StoredChordDisplay _storedChordDisplay;
    public override RelicRarity Rarity => RelicRarity.Starter;

    protected virtual int MaxLearnedChordsPerCategory => 1;
    protected virtual int EffectMultiplier => 1;


    protected override IEnumerable<string> RegisteredKeywordIds => [CutesakiKeywords.RememberChord];

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

    public IReadOnlyList<string> GetTemporaryChords()
    {
        return _temporaryChords.AsReadOnly();
    }

    public int GetEffectMultiplier()
    {
        return EffectMultiplier;
    }


    // ========== Bonus 管理 ==========
    public IReadOnlyList<string> GetBonusChords()
    {
        return _bonusChords.AsReadOnly();
    }

    public void AddBonusChord(string chordId)
    {
        if (!ChordManager.AllChords.ContainsKey(chordId)) return;
        _bonusChords.Add(chordId);
        Flash();
    }

    public bool RemoveBonusChord(string chordId)
    {
        var removed = _bonusChords.Remove(chordId);
        if (removed) Flash();
        return removed;
    }

    public bool HasBonusChord()
    {
        return _bonusChords.Count > 0;
    }

    public string GetBonusChord()
    {
        return _bonusChords.FirstOrDefault();
    }

    public void SetBonusChord(string chordId)
    {
        if (!ChordManager.AllChords.ContainsKey(chordId)) return;
        _bonusChords.Clear();
        _bonusChords.Add(chordId);
        Flash();
    }

    // ========== 临时替换 ==========
    public void TempReplaceChord(ChordCategory category, string tempChordId)
    {
        if (!ChordManager.AllChords.TryGetValue(tempChordId, out var def) || !def.IsTemporaryOnly)
            return;
        if (!_currentChords.ContainsKey(category))
            return;

        _preTempChords ??= new Dictionary<ChordCategory, string>();
        if (!_preTempChords.ContainsKey(category))
            _preTempChords[category] = _currentChords[category];

        _currentChords[category] = tempChordId;
        Flash();
    }

    public void RestoreTempChords()
    {
        if (_preTempChords == null) return;
        foreach (var kv in _preTempChords)
            if (_currentChords.ContainsKey(kv.Key))
                _currentChords[kv.Key] = kv.Value;
        _preTempChords = null;
        Flash();
    }

    // ========== 生命周期 ==========
    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        if (!_initialized)
        {
            _currentChords ??= new Dictionary<ChordCategory, string>();
            _bonusChords ??= new List<string>();

            if (string.IsNullOrEmpty(_currentChords.GetValueOrDefault(ChordCategory.Major)))
                _currentChords[ChordCategory.Major] = ChordManager.GetBaseChordId(ChordCategory.Major);
            if (string.IsNullOrEmpty(_currentChords.GetValueOrDefault(ChordCategory.Minor)))
                _currentChords[ChordCategory.Minor] = ChordManager.GetBaseChordId(ChordCategory.Minor);
            if (string.IsNullOrEmpty(_currentChords.GetValueOrDefault(ChordCategory.Dominant)))
                _currentChords[ChordCategory.Dominant] = ChordManager.GetBaseChordId(ChordCategory.Dominant);

            _initialized = true;
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

    // 在 AnonGuitar.cs 的 NotifyChordPlayed 方法中替换或追加以下逻辑
    private async Task NotifyChordPlayed(PlayerChoiceContext choiceContext)
    {
        // 1. 通知“难忘的演奏”能力
        foreach (var power in Owner.Creature.Powers.OfType<UnforgettablePerformancePower>())
            await power.OnChordPlayed(choiceContext);

        // 2. 谢幕卡牌回收逻辑：将所有“谢幕”从其他牌堆移回手牌
        const string curtainCallId = "CUTESAKIKOMOD-CURTAIN_CALL";
        var player = Owner;
        if (player == null) return;

        // 收集所有需要移回手牌的谢幕卡牌
        var cardsToMove = new List<CardModel>();
        var searchPiles = new[] { PileType.Discard, PileType.Draw, PileType.Exhaust };

        foreach (var pileType in searchPiles)
        {
            var pile = pileType.GetPile(player);
            if (pile == null) continue;
            // 找出所有匹配的谢幕卡牌
            cardsToMove.AddRange(pile.Cards.Where(c => c.Id.Entry == curtainCallId));
        }

        // 一次性将它们移回手牌
        foreach (var card in cardsToMove)
            // CardPileCmd.Add 会自动处理牌堆转移，无需先移除
            await CardPileCmd.Add(card, PileType.Hand);

        if (cardsToMove.Count > 0)
            Flash(); // 吉他闪烁提示
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

    // ========== 核心逻辑 ==========
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Owner != Owner) return;
        if (Owner.Creature.CombatState == null) return;

        var noNoteKeyword = CutesakiKeywords.NoNote;
        if (noNoteKeyword != null && cardPlay.Card.HasModKeyword(noNoteKeyword))
        {
            UpdateNoteDisplay();
            UpdateStoredChordDisplay();
            return;
        }

        // 意犹未尽：记录当前储存是否已满，若是则保留最早和弦的ID
        var storedBefore = MusicNoteManager.GetStoredChords(Owner);
        string? overflowChordId = null;
        var hasLingering = Owner.Creature.HasPower<LingeringTastePower>();
        if (hasLingering && storedBefore.Count >= MusicNoteManager.MaxStoredChords && storedBefore.Count > 0)
            overflowChordId = storedBefore[0]; // 最早加入的和弦

        var newChords = MusicNoteManager.AddNote(Owner, cardPlay.Card.Type, _currentChords,
            _bonusChords.Concat(_temporaryChords));

        // 意犹未尽：如果生成了新和弦且原本储存已满，则自动演奏溢出的和弦
        if (newChords.Count > 0 && overflowChordId != null)
            if (ChordManager.AllChords.TryGetValue(overflowChordId, out var overflowDef))
                await overflowDef.Effect(choiceContext, Owner.Creature, EffectMultiplier);

        var hasAutoPlay = Owner.Creature.HasPower<PlayImmediatelyPower>();
        if (hasAutoPlay && newChords.Count > 0)
        {
            foreach (var chordId in newChords)
            {
                // 使用 ChordEffectPlayer 播放特效
                _ = ChordEffectPlayer.PlayChordIcons(Owner.Creature, new[] { chordId }, 0f);

                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                {
                    await def.Effect(choiceContext, Owner.Creature, EffectMultiplier);
                    MusicNoteManager.RemoveChord(Owner, chordId);
                }
            }

            await NotifyChordPlayed(choiceContext);
        }

        if (newChords.Count == 0)
            foreach (var power in Owner.Creature.Powers.OfType<StageNervesPower>())
                await power.OnNoteWithoutChord();

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

    public async Task TriggerAllStoredChords(PlayerChoiceContext choiceContext)
    {
        var stored = MusicNoteManager.GetStoredChords(Owner);
        foreach (var chordId in stored)
        {
            _ = ChordEffectPlayer.PlayChordIcons(Owner.Creature, new[] { chordId }, 0f);
            if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                await def.Effect(choiceContext, Owner.Creature, EffectMultiplier);
        }

        ClearSequence();
        await NotifyChordPlayed(choiceContext);
    }

    public async Task TriggerAllStoredChordsKeepNotes(PlayerChoiceContext choiceContext)
    {
        var stored = MusicNoteManager.GetStoredChords(Owner);
        foreach (var chordId in stored)
        {
            _ = ChordEffectPlayer.PlayChordIcons(Owner.Creature, new[] { chordId }, 0f);
            if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                await def.Effect(choiceContext, Owner.Creature, EffectMultiplier);
        }

        MusicNoteManager.ClearStoredChords(Owner);
        UpdateStoredChordDisplay();
        await NotifyChordPlayed(choiceContext);
    }

    public async Task AddChordToStored(PlayerChoiceContext choiceContext, string chordId)
    {
        if (!ChordManager.AllChords.ContainsKey(chordId)) return;

        MusicNoteManager.AddChordDirectly(Owner, chordId);

        var hasAutoPlay = Owner.Creature.HasPower<PlayImmediatelyPower>();
        if (hasAutoPlay)
        {
            _ = ChordEffectPlayer.PlayChordIcons(Owner.Creature, new[] { chordId }, 0f);
            if (ChordManager.AllChords.TryGetValue(chordId, out var def))
            {
                await def.Effect(choiceContext, Owner.Creature, EffectMultiplier);
                MusicNoteManager.RemoveChord(Owner, chordId);
            }
        }

        UpdateStoredChordDisplay();
    }

    public List<string> GetLearnedChordIds(params ChordCategory[] categories)
    {
        var result = new List<string>();
        var filter = categories.Length > 0 ? new HashSet<ChordCategory>(categories) : null;

        foreach (var kv in _currentChords)
        {
            if (string.IsNullOrEmpty(kv.Value)) continue;
            if (kv.Key == ChordCategory.Bonus) continue;
            if (filter == null || filter.Contains(kv.Key))
                result.Add(kv.Value);
        }

        if ((filter == null || filter.Contains(ChordCategory.Bonus)) && _bonusChords.Count > 0)
            result.AddRange(_bonusChords);

        // 添加临时和弦
        if (_temporaryChords.Count > 0)
            result.AddRange(_temporaryChords);

        return result;
    }

    public async Task TriggerLearnedChords(PlayerChoiceContext choiceContext, params ChordCategory[] categories)
    {
        var chordIds = GetLearnedChordIds(categories);
        foreach (var chordId in chordIds)
        {
            _ = ChordEffectPlayer.PlayChordIcons(Owner.Creature, new[] { chordId }, 0f);
            if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                await def.Effect(choiceContext, Owner.Creature, EffectMultiplier);
        }
    }

    public async Task TriggerAllLearnedChords(PlayerChoiceContext choiceContext)
    {
        await TriggerLearnedChords(choiceContext);
    }

    public async Task TriggerLastStoredChord(PlayerChoiceContext choiceContext)
    {
        var stored = MusicNoteManager.GetStoredChords(Owner);
        if (stored.Count == 0) return;

        var lastChordId = stored.Last();
        _ = ChordEffectPlayer.PlayChordIcons(Owner.Creature, new[] { lastChordId }, 0f);
        if (ChordManager.AllChords.TryGetValue(lastChordId, out var def))
            await def.Effect(choiceContext, Owner.Creature, EffectMultiplier);

        MusicNoteManager.RemoveChord(Owner, lastChordId);
        UpdateStoredChordDisplay();
        await NotifyChordPlayed(choiceContext);
    }

    public void ReplaceChord(ChordCategory category, string newChordId)
    {
        if (!ChordManager.AllChords.ContainsKey(newChordId)) return;
        _currentChords[category] = newChordId;
        Flash();
    }

    /// <summary>添加一个临时已记忆和弦</summary>
    public void AddTemporaryChord(string chordId)
    {
        if (!ChordManager.AllChords.ContainsKey(chordId)) return;
        _temporaryChords.Add(chordId);
        Flash();
    }

    /// <summary>移除指定的临时和弦</summary>
    public bool RemoveTemporaryChord(string chordId)
    {
        var removed = _temporaryChords.Remove(chordId);
        if (removed) Flash();
        return removed;
    }

    /// <summary>清除所有临时和弦</summary>
    public void ClearTemporaryChords()
    {
        if (_temporaryChords.Count == 0) return;
        _temporaryChords.Clear();
        Flash();
    }

    public IReadOnlyDictionary<ChordCategory, string> GetCurrentChords()
    {
        return _currentChords;
    }

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

    public override async Task AfterRemoved()
    {
        if (_bonusChords.Count > 0) _pendingBonusTransfer[Owner] = new List<string>(_bonusChords);

        CleanupUI();
        MusicNoteManager.ClearAll(Owner);
        await base.AfterRemoved();
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        RestoreTempChords();
        _temporaryChords.Clear(); // 清除碧天伴走等临时和弦
        MusicNoteManager.ClearCombatData(Owner);
        CleanupUI();
        await base.AfterCombatEnd(room);
    }

    public void CopyChordsTo(AnonGuitar target)
    {
        foreach (var kv in _currentChords)
            target._currentChords[kv.Key] = kv.Value;
        target._bonusChords = new List<string>(_bonusChords);
    }
}