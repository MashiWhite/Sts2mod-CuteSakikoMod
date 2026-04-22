
using CuteSakikoMod.CuteSakikoModCode.Events;
using CuteSakikoMod.CuteSakikoModCode.Nodes;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic
{
    public class AnonGuitar : CuteAnonRelic
    {
        public override RelicRarity Rarity => RelicRarity.Starter;

        [SavedProperty]
        private Dictionary<ChordCategory, string> _currentChords = new()
        {
            [ChordCategory.Major] = "C",
            [ChordCategory.Minor] = "Am",
            [ChordCategory.Dominant] = "G7"
        };

        [SavedProperty]
        private List<string> _bonusChords = new();

        [SavedProperty]
        private bool _initialized;

        private NoteDisplay _noteDisplay;
        private StoredChordDisplay _storedChordDisplay;
        private Dictionary<ChordCategory, string>? _preTempChords;

        // 将 private 改为 protected
        protected static Dictionary<Player, List<string>> _pendingBonusTransfer = new();
        
        protected virtual int MaxLearnedChordsPerCategory => 1;
        protected virtual int EffectMultiplier => 1;

        // ========== Bonus 管理 ==========
        public IReadOnlyList<string> GetBonusChords() => _bonusChords.AsReadOnly();

        public void AddBonusChord(string chordId)
        {
            if (!ChordManager.AllChords.ContainsKey(chordId)) return;
            _bonusChords.Add(chordId);
            Flash();
        }

        public bool RemoveBonusChord(string chordId)
        {
            bool removed = _bonusChords.Remove(chordId);
            if (removed) Flash();
            return removed;
        }

        public bool HasBonusChord() => _bonusChords.Count > 0;

        public string GetBonusChord() => _bonusChords.FirstOrDefault();

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
            {
                if (_currentChords.ContainsKey(kv.Key))
                    _currentChords[kv.Key] = kv.Value;
            }
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

        // ========== UI 管理（完整实现） ==========
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

        private async Task NotifyChordPlayed(PlayerChoiceContext choiceContext)
        {
            foreach (var power in Owner.Creature.Powers.OfType<UnforgettablePerformancePower>())
            {
                await power.OnChordPlayed(choiceContext);
            }
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

        private void UpdateStoredChordDisplay()
        {
            if (_storedChordDisplay != null && GodotObject.IsInstanceValid(_storedChordDisplay))
            {
                var stored = MusicNoteManager.GetStoredChords(Owner).ToList();
                _storedChordDisplay.UpdateChords(stored, EffectMultiplier);
            }
            else if (Owner?.Creature?.CombatState != null)
                EnsureStoredChordDisplay();
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

            CardKeyword noNoteKeyword = CutesakiKeywords.NoNote;
            if (noNoteKeyword != null && cardPlay.Card.Keywords.Contains(noNoteKeyword))
            {
                UpdateNoteDisplay();
                UpdateStoredChordDisplay();
                return;
            }

            var newChords = MusicNoteManager.AddNote(Owner, cardPlay.Card.Type, _currentChords, _bonusChords);

            bool hasAutoPlay = Owner.Creature.HasPower<PlayImmediatelyPower>();
            if (hasAutoPlay && newChords.Count > 0)
            {
                foreach (var chordId in newChords)
                {
                    if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                    {
                        await def.Effect(choiceContext, Owner.Creature, EffectMultiplier);
                        MusicNoteManager.RemoveChord(Owner, chordId);
                    }
                }
                await NotifyChordPlayed(choiceContext);
            }

            if (newChords.Count == 0)
            {
                foreach (var power in Owner.Creature.Powers.OfType<StageNervesPower>())
                    await power.OnNoteWithoutChord();
            }

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
                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                    await def.Effect(choiceContext, Owner.Creature, EffectMultiplier);
            }
            ClearSequence();
            await NotifyChordPlayed(choiceContext);
        }

        public async Task AddChordToStored(PlayerChoiceContext choiceContext, string chordId)
        {
            if (!ChordManager.AllChords.ContainsKey(chordId)) return;

            MusicNoteManager.AddChordDirectly(Owner, chordId);

            bool hasAutoPlay = Owner.Creature.HasPower<PlayImmediatelyPower>();
            if (hasAutoPlay)
            {
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

            return result;
        }

        public async Task TriggerLearnedChords(PlayerChoiceContext choiceContext, params ChordCategory[] categories)
        {
            var chordIds = GetLearnedChordIds(categories);
            foreach (var chordId in chordIds)
                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                    await def.Effect(choiceContext, Owner.Creature, EffectMultiplier);
        }

        public async Task TriggerAllLearnedChords(PlayerChoiceContext choiceContext) =>
            await TriggerLearnedChords(choiceContext);

        public async Task TriggerLastStoredChord(PlayerChoiceContext choiceContext)
        {
            var stored = MusicNoteManager.GetStoredChords(Owner);
            if (stored.Count == 0) return;

            var lastChordId = stored.Last();
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

        public IReadOnlyDictionary<ChordCategory, string> GetCurrentChords() => _currentChords;

        // ========== 悬浮提示 ==========
        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                if (!this.IsMutable) yield break;

                var desc = new LocString("relics", "ANON_GUITAR_CHORDS_DESC");
                var lines = new List<string>();

                foreach (var kv in _currentChords.Where(kv => kv.Key != ChordCategory.Bonus))
                {
                    if (ChordManager.AllChords.TryGetValue(kv.Value, out var def))
                    {
                        string title = new LocString("card_keywords", def.TitleKey).GetFormattedText();
                        string text = ChordDisplayHelper.GetFormattedDescription(def, EffectMultiplier);
                        lines.Add($"[{title}]({def.GetConditionText()})\n{text}");
                    }
                }

                foreach (var chordId in _bonusChords)
                {
                    if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                    {
                        string title = new LocString("card_keywords", def.TitleKey).GetFormattedText();
                        string text = ChordDisplayHelper.GetFormattedDescription(def, EffectMultiplier);
                        lines.Add($"[{title}]({def.GetConditionText()})\n{text}");
                    }
                }

                desc.Add("Chords", string.Join("\n\n", lines));
                yield return new HoverTip(new LocString("relics", "ANON_GUITAR_CHORDS_TITLE"), desc);
            }
        }

        public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
        {
            if (player != Owner) return false;
            bool canLearn = ChordManager.GetLearnableChordIds(ChordCategory.Major).Count > 0 ||
                            ChordManager.GetLearnableChordIds(ChordCategory.Minor).Count > 0 ||
                            ChordManager.GetLearnableChordIds(ChordCategory.Dominant).Count > 0;
            if (!canLearn) return false;
            options.Add(new PracticeGuitarOption(player, this));
            return true;
        }
        
        /// <summary>
        /// 演奏所有已储存的和弦，但不清除音符序列（保留当前音符队列）。
        /// </summary>
        public async Task TriggerAllStoredChordsKeepNotes(PlayerChoiceContext choiceContext)
        {
            var stored = MusicNoteManager.GetStoredChords(Owner);
            foreach (var chordId in stored)
            {
                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                {
                    await def.Effect(choiceContext, Owner.Creature, EffectMultiplier);
                }
            }
            // 清空储存队列，但保留音符
            MusicNoteManager.ClearStoredChords(Owner);
            UpdateStoredChordDisplay();

            await NotifyChordPlayed(choiceContext);
        }

        public override async Task AfterRemoved()
        {
            // 将当前 Bonus 保存到缓存，供替换遗物（如闪亮吉他）继承
            if (_bonusChords.Count > 0)
            {
                _pendingBonusTransfer[Owner] = new List<string>(_bonusChords);
            }

            CleanupUI();
            MusicNoteManager.ClearAll(Owner);
            await base.AfterRemoved();
        }

        public override async Task AfterCombatEnd(CombatRoom room)
        {
            RestoreTempChords();
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
}