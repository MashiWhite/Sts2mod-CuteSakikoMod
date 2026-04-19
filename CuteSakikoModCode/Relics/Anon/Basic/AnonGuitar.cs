using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using CuteSakikoMod.CuteSakikoModCode.Events;
using CuteSakikoMod.CuteSakikoModCode.Nodes;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Pools.Anon;
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

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic
{
    [Pool(typeof(CuteAnonRelicPool))]
    public class AnonGuitar : CuteAnonRelic
    {
        public override RelicRarity Rarity => RelicRarity.Starter;

        // 主槽位字典，增加一个 Bonus 键用于存储额外槽位
        [SavedProperty]
        private Dictionary<ChordCategory, string> _currentChords = new()
        {
            [ChordCategory.Major] = "C",
            [ChordCategory.Minor] = "Am",
            [ChordCategory.Dominant] = "G7"
            // Bonus 键默认不存在，表示无额外槽位
        };

        [SavedProperty]
        private bool _initialized;

        private NoteDisplay _noteDisplay;
        private StoredChordDisplay _storedChordDisplay;

        // 保存临时替换前的原始和弦，用于战斗结束时恢复
        private Dictionary<ChordCategory, string>? _preTempChords;

        protected virtual int MaxLearnedChordsPerCategory => 1;
        protected virtual int EffectMultiplier => 1;

        // ========== 额外槽位辅助方法（操作字典中的 Bonus 键） ==========
        public string GetBonusChord()
        {
            return _currentChords.TryGetValue(ChordCategory.Bonus, out var chord) ? chord : null;
        }

        public void SetBonusChord(string chordId)
        {
            if (!ChordManager.AllChords.ContainsKey(chordId)) return;
            _currentChords[ChordCategory.Bonus] = chordId;
            Flash();
        }

        public bool HasBonusChord()
        {
            return _currentChords.ContainsKey(ChordCategory.Bonus) &&
                   !string.IsNullOrEmpty(_currentChords[ChordCategory.Bonus]);
        }

        // ========== 临时替换逻辑 ==========
        /// <summary>
        /// 将指定分类的当前和弦临时替换为指定的临时和弦ID。
        /// 战斗结束后会自动恢复为原始和弦。
        /// </summary>
        /// <param name="category">要替换的分类，例如 ChordCategory.Major</param>
        /// <param name="tempChordId">临时和弦ID，必须是 ChordManager 中注册的临时和弦</param>
        public void TempReplaceChord(ChordCategory category, string tempChordId)
        {
            if (!ChordManager.AllChords.TryGetValue(tempChordId, out var def) || !def.IsTemporaryOnly)
                return;
            if (!_currentChords.ContainsKey(category))
                return;

            // 保存原始和弦（如果尚未保存该分类）
            _preTempChords ??= new Dictionary<ChordCategory, string>();
            if (!_preTempChords.ContainsKey(category))
                _preTempChords[category] = _currentChords[category];

            _currentChords[category] = tempChordId;
            Flash();
        }

        /// <summary>
        /// 恢复所有被临时替换的分类到原始和弦。
        /// </summary>
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
                // 确保字典存在
                _currentChords ??= new Dictionary<ChordCategory, string>();

                // 补全三个主槽位
                if (string.IsNullOrEmpty(_currentChords.GetValueOrDefault(ChordCategory.Major)))
                    _currentChords[ChordCategory.Major] = ChordManager.GetBaseChordId(ChordCategory.Major);
                if (string.IsNullOrEmpty(_currentChords.GetValueOrDefault(ChordCategory.Minor)))
                    _currentChords[ChordCategory.Minor] = ChordManager.GetBaseChordId(ChordCategory.Minor);
                if (string.IsNullOrEmpty(_currentChords.GetValueOrDefault(ChordCategory.Dominant)))
                    _currentChords[ChordCategory.Dominant] = ChordManager.GetBaseChordId(ChordCategory.Dominant);

                // 注意：额外槽位（Bonus）在此处不主动初始化，由子类或休息处处理

                _initialized = true;
            }
        }

        // ========== UI 管理 ==========
        private void EnsureNoteDisplay()
        {
            if (_noteDisplay != null && GodotObject.IsInstanceValid(_noteDisplay)) return;
            if (Owner.Creature.CombatState == null) return;
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

        private void UpdateNoteDisplay()
        {
            if (_noteDisplay != null && GodotObject.IsInstanceValid(_noteDisplay))
                _noteDisplay.UpdateNotes(MusicNoteManager.GetCurrentNotes(Owner));
            else if (Owner.Creature.CombatState != null)
                EnsureNoteDisplay();
        }

        private void EnsureStoredChordDisplay()
        {
            if (_storedChordDisplay != null && GodotObject.IsInstanceValid(_storedChordDisplay)) return;
            if (Owner.Creature.CombatState == null) return;
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
            else if (Owner.Creature.CombatState != null)
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

            // 获取 NoNote 关键词，并显式声明为 CardKeyword 类型以帮助编译器
            CardKeyword noNoteKeyword = CutesakiKeywords.NoNote;
            bool shouldSkipNote = noNoteKeyword != null && cardPlay.Card.Keywords.Contains(noNoteKeyword);

            if (!shouldSkipNote)
            {
                var newChords = MusicNoteManager.AddNote(Owner, cardPlay.Card.Type, _currentChords);

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
                }
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
        }
        
        /// <summary>
        /// 获取所有已学习和弦的 ID 列表（可选按分类筛选）
        /// </summary>
        public List<string> GetLearnedChordIds(params ChordCategory[] categories)
        {
            var result = new List<string>();
            var filter = categories.Length > 0 ? new HashSet<ChordCategory>(categories) : null;

            // 三个主槽位
            foreach (var kv in _currentChords)
            {
                if (string.IsNullOrEmpty(kv.Value)) continue;
                if (kv.Key == ChordCategory.Bonus) continue;
                if (filter == null || filter.Contains(kv.Key))
                    result.Add(kv.Value);
            }

            // 额外槽位（如果请求的分类包含 Bonus，或未指定分类）
            if ((filter == null || filter.Contains(ChordCategory.Bonus)) && HasBonusChord())
                result.Add(GetBonusChord());

            return result;
        }

        /// <summary>
        /// 演奏指定分类的已学习和弦（不分类则演奏全部）
        /// </summary>
        public async Task TriggerLearnedChords(PlayerChoiceContext choiceContext, params ChordCategory[] categories)
        {
            var chordIds = GetLearnedChordIds(categories);
            foreach (var chordId in chordIds)
            {
                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                    await def.Effect(choiceContext, Owner.Creature, EffectMultiplier);
            }
        }

        /// <summary>
        /// 演奏所有已学习和弦（便捷方法）
        /// </summary>
        public async Task TriggerAllLearnedChords(PlayerChoiceContext choiceContext)
        {
            await TriggerLearnedChords(choiceContext);
        }
        
        /// <summary>
        /// 演奏最新储存的一个和弦（队列尾部），并消耗该和弦。
        /// </summary>
        public async Task TriggerLastStoredChord(PlayerChoiceContext choiceContext)
        {
            var stored = MusicNoteManager.GetStoredChords(Owner);
            if (stored.Count == 0) return;

            var lastChordId = stored.Last();
            if (ChordManager.AllChords.TryGetValue(lastChordId, out var def))
                await def.Effect(choiceContext, Owner.Creature, EffectMultiplier);

            MusicNoteManager.RemoveChord(Owner, lastChordId);
            UpdateStoredChordDisplay();
        }

        public void ReplaceChord(ChordCategory category, string newChordId)
        {
            if (!ChordManager.AllChords.ContainsKey(newChordId)) return;
            _currentChords[category] = newChordId;
            Flash();
        }

        public IReadOnlyDictionary<ChordCategory, string> GetCurrentChords() => _currentChords;

        protected override IEnumerable<IHoverTip> ExtraHoverTips
        {
            get
            {
                var tips = new List<IHoverTip>();
                var desc = new LocString("relics", "ANON_GUITAR_CHORDS_DESC");
                var lines = new List<string>();

                // 三个主槽位（排除 Bonus 键）
                foreach (var kv in _currentChords.Where(kv => kv.Key != ChordCategory.Bonus))
                {
                    if (ChordManager.AllChords.TryGetValue(kv.Value, out var def))
                    {
                        string titleText = new LocString("card_keywords", def.TitleKey).GetFormattedText();
                        string descText = ChordDisplayHelper.GetFormattedDescription(def, EffectMultiplier);
                        lines.Add($"[{titleText}]({def.GetConditionText()})\n{descText}");
                    }
                }

                // 额外槽位（如果存在）
                if (HasBonusChord() && ChordManager.AllChords.TryGetValue(GetBonusChord(), out var bonusDef))
                {
                    string titleText = new LocString("card_keywords", bonusDef.TitleKey).GetFormattedText();
                    string descText = ChordDisplayHelper.GetFormattedDescription(bonusDef, EffectMultiplier);
                    lines.Add($"[{titleText}]({bonusDef.GetConditionText()})\n{descText}");
                }

                desc.Add("Chords", string.Join("\n\n", lines));
                tips.Add(new HoverTip(new LocString("relics", "ANON_GUITAR_CHORDS_TITLE"), desc));
                return tips;
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

        public override async Task AfterRemoved()
        {
            CleanupUI();
            await base.AfterRemoved();
        }

        public override async Task AfterCombatEnd(CombatRoom room)
        {
            RestoreTempChords(); // 战斗结束时恢复临时替换
            MusicNoteManager.ClearAll(Owner);
            CleanupUI();
            await base.AfterCombatEnd(room);
        }

        public override RelicModel? GetUpgradeReplacement()
        {
            return ModelDb.Relic<FlashAnonGuitar>().ToMutable();
        }

        public void CopyChordsTo(AnonGuitar target)
        {
            foreach (var kv in _currentChords)
                target._currentChords[kv.Key] = kv.Value;
        }
    }
}