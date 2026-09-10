using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Uncommon;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Utils;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Chord
{
    public static class ChordNoteSystem
    {
        public const int MaxStoredChords = 3;

        private static readonly SavedAttachedState<Player, string> _savedState =
            new("chord_note_state", defaultValueFactory: () => "");

        private static readonly AttachedState<Player, List<IChordProvider>> _providers =
            new(() => new List<IChordProvider>());

        public static event Action<Player>? PlayerNotesChanged;

        private sealed class StateData
        {
            public bool IsActive;
            public Queue<CardType> Notes = new();
            public List<string> StoredChords = new();
            public int LastRoundNumber;
            public int NotesGainedThisTurn;
            public int TotalNotesThisCombat;
            public bool FirstPlayBonusConsumedThisTurn;
            public bool FirstPlayBonusAppliedThisOperation;
            public bool ChordBonusConsumedThisOperation;
            public bool CurtainCallRecalledThisTurn;
        }

        private static StateData Deserialize(string data)
        {
            var state = new StateData();
            if (string.IsNullOrEmpty(data)) return state;

            var parts = data.Split('|');
            if (parts.Length < 10) return state;

            bool.TryParse(parts[0], out state.IsActive);
            int.TryParse(parts[1], out state.LastRoundNumber);
            int.TryParse(parts[2], out state.NotesGainedThisTurn);
            int.TryParse(parts[3], out state.TotalNotesThisCombat);

            if (!string.IsNullOrEmpty(parts[4]))
            {
                foreach (var s in parts[4].Split(','))
                    if (int.TryParse(s, out var type) && Enum.IsDefined(typeof(CardType), type))
                        state.Notes.Enqueue((CardType)type);
            }

            if (!string.IsNullOrEmpty(parts[5]))
                state.StoredChords.AddRange(parts[5].Split(';', StringSplitOptions.RemoveEmptyEntries));

            bool.TryParse(parts[6], out state.FirstPlayBonusConsumedThisTurn);
            bool.TryParse(parts[7], out state.FirstPlayBonusAppliedThisOperation);
            bool.TryParse(parts[8], out state.ChordBonusConsumedThisOperation);
            bool.TryParse(parts[9], out state.CurtainCallRecalledThisTurn);

            return state;
        }

        private static string Serialize(StateData state)
        {
            var notes = string.Join(",", state.Notes.Select(n => (int)n));
            var chords = string.Join(";", state.StoredChords);
            return $"{state.IsActive}|{state.LastRoundNumber}|{state.NotesGainedThisTurn}|{state.TotalNotesThisCombat}|{notes}|{chords}|{state.FirstPlayBonusConsumedThisTurn}|{state.FirstPlayBonusAppliedThisOperation}|{state.ChordBonusConsumedThisOperation}|{state.CurtainCallRecalledThisTurn}";
        }

        private static StateData GetState(Player player)
        {
            var raw = _savedState[player];
            return Deserialize(raw);
        }

        private static void SaveState(Player player, StateData state)
        {
            _savedState[player] = Serialize(state);
        }

        private static AnonGuitar? GetGuitar(Player player)
            => player?.Relics?.OfType<AnonGuitar>().FirstOrDefault();

        private static int GetFirstPlayBonus(Player player)
            => GetGuitar(player)?.FirstPlayBonus ?? 0;

        private static int GetBaseChordBonus(Player player)
            => GetGuitar(player)?.BaseChordBonus ?? 0;

        // ==================== 激活/停用 ====================

        public static void Activate(Player player)
        {
            if (player == null) return;
            var state = GetState(player);
            state.IsActive = true;
            SaveState(player, state);
        }

        public static void Deactivate(Player player)
        {
            if (player == null) return;
            var state = GetState(player);
            state.IsActive = false;
            state.Notes.Clear();
            state.StoredChords.Clear();
            if (_providers.TryGetValue(player, out var providers))
                providers.Clear();
            SaveState(player, state);
        }

        // ==================== Provider ====================

        public static void RegisterProvider(Player player, IChordProvider provider)
        {
            if (player == null || provider == null) return;
            var providers = _providers[player];
            if (!providers.Contains(provider))
                providers.Add(provider);
        }

        public static void UnregisterProvider(Player player, IChordProvider provider)
        {
            if (player == null || provider == null) return;
            _providers[player].Remove(provider);
        }

        private static IReadOnlyList<string> GetAvailableChordIds(Player player)
        {
            var providers = _providers[player];
            return providers.SelectMany(p => p.GetAvailableChordIds(player)).Distinct().ToList();
        }

        // ==================== Bonus 计算 ====================

        private static void TryConsumeFirstPlayBonus(StateData state, Player player)
        {
            int firstPlayBonus = GetFirstPlayBonus(player);
            if (!state.FirstPlayBonusConsumedThisTurn && firstPlayBonus > 0)
            {
                state.FirstPlayBonusConsumedThisTurn = true;
                state.FirstPlayBonusAppliedThisOperation = true;
            }
        }

        private static void BeginPlayOperation(StateData state, Player player)
        {
            state.FirstPlayBonusAppliedThisOperation = false;
            state.ChordBonusConsumedThisOperation = false;
            TryConsumeFirstPlayBonus(state, player);
        }

        private static int CalculateBaseBonus(Player player)
        {
            int bonus = GetBaseChordBonus(player);
            if (player?.Creature == null) return bonus;

            foreach (var provider in player.Creature.Powers.OfType<IChordBonusProvider>())
                bonus += provider.GetBonus();
            foreach (var provider in player.Relics.OfType<IChordBonusProvider>())
                bonus += provider.GetBonus();

            var chordBonusPower = player.Creature.GetPower<ChordBonusPower>();
            if (chordBonusPower != null && chordBonusPower.Amount > 0)
                bonus += 1;

            return bonus;
        }

        private static int CalculateTotalBonus(Player player, StateData state)
        {
            int bonus = CalculateBaseBonus(player);
            int firstPlayBonus = GetFirstPlayBonus(player);
            if ((!state.FirstPlayBonusConsumedThisTurn && firstPlayBonus > 0) || state.FirstPlayBonusAppliedThisOperation)
                bonus += firstPlayBonus;
            return bonus;
        }
        
        public static int GetDisplayBonus(Player player)
        {
            if (player?.Creature?.CombatState == null)
                return GetBaseChordBonus(player);
            var state = GetState(player);
            return CalculateTotalBonus(player, state);
        }

        // ==================== 添加音符 ====================

        public static async Task AddNoteAsync(Player player, CardType noteType, PlayerChoiceContext context)
        {
            var state = GetState(player);
            if (!state.IsActive || player.Creature?.CombatState == null) return;

            var combat = player.Creature.CombatState;
            if (state.LastRoundNumber != combat.RoundNumber)
            {
                state.LastRoundNumber = combat.RoundNumber;
                state.NotesGainedThisTurn = 0;
            }

            await ChordNoteHooks.BeforeNoteAdded(combat, player, noteType, context);

            state.NotesGainedThisTurn++;
            state.TotalNotesThisCombat++;
            state.Notes.Enqueue(noteType);
            while (state.Notes.Count > 4) state.Notes.Dequeue();

            await ChordNoteHooks.AfterNoteAdded(combat, player, noteType, context);

            SaveState(player, state);

            // 触发 GuitarVocalPower
            var vocalPower = player.Creature.GetPower<GuitarVocalPower>();
            if (vocalPower != null)
                await vocalPower.OnNoteGained(context, 1);

            // 匹配和弦并自动演奏
            await TryMatchAndAutoPlayAsync(player, context, state);
            PlayerNotesChanged?.Invoke(player);
        }

        private static async Task TryMatchAndAutoPlayAsync(Player player, PlayerChoiceContext context, StateData state)
        {
            var available = GetAvailableChordIds(player);
            if (available.Count == 0) return;

            var sequence = state.Notes.ToList();
            var matched = new List<string>();
            foreach (var chordId in available)
            {
                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                {
                    var modifiedSeq = ChordSequenceModifierHelper.GetModifiedSequence(def, player.Creature);
                    if (ChordManager.MatchesChord(modifiedSeq, sequence))
                        matched.Add(chordId);
                }
            }

            foreach (var chordId in matched)
            {
                await ChordNoteHooks.BeforeChordMatched(player.Creature.CombatState, player, chordId, context);
                state.StoredChords.Add(chordId);
                await ChordNoteHooks.AfterChordMatched(player.Creature.CombatState, player, chordId, context);
            }

            // 处理溢出：记录最老的溢出和弦，只记录第一个（与原版 MusicNoteManager 一致）
            string? overflowChord = null;
            while (state.StoredChords.Count > MaxStoredChords)
            {
                overflowChord ??= state.StoredChords[0];
                state.StoredChords.RemoveAt(0);
            }

            SaveState(player, state);

            await AutoPlayNewChordsAsync(player, context, state, matched, overflowChord);
        }

        private static async Task AutoPlayNewChordsAsync(
            Player player, PlayerChoiceContext context, StateData state,
            List<string> newChords, string? overflowChord)
        {
            // 溢出且立即演奏（需要 LingeringTastePower）
            if (overflowChord != null && player.Creature.HasPower<LingeringTastePower>())
            {
                BeginPlayOperation(state, player);
                await PlaySingleChordInternalAsync(player, context, state, overflowChord, 1, removeStored: false);
                SaveState(player, state);
                return;
            }

            // 拥有 PlayImmediatelyPower 且存在新和弦 → 立即演奏
            var playImmediately = player.Creature.GetPower<PlayImmediatelyPower>();
            if (playImmediately != null && playImmediately.Amount > 0 && newChords.Count > 0)
            {
                BeginPlayOperation(state, player);
                foreach (var chordId in newChords.ToList())
                {
                    if (playImmediately.Amount <= 0) break;
                    await PlaySingleChordInternalAsync(player, context, state, chordId, 1, removeStored: false);
                    RemoveChordFromStored(state, chordId);
                    await PowerCmd.Decrement(playImmediately);
                }
                SaveState(player, state);
            }
            else if (newChords.Count == 0)
            {
                // 无新和弦 → StageNerves，不演奏也不消耗加成
                foreach (var power in player.Creature.Powers.OfType<StageNervesPower>())
                    await power.OnNoteWithoutChord();
            }
        }

        private static void RemoveChordFromStored(StateData state, string chordId)
        {
            var index = state.StoredChords.FindLastIndex(c => c == chordId);
            if (index >= 0) state.StoredChords.RemoveAt(index);
        }

        // ==================== 核心演奏 ====================

        private static async Task PlaySingleChordInternalAsync(
            Player player, PlayerChoiceContext context, StateData state,
            string chordId, int count = 1, bool removeStored = true)
        {
            var chordBonusPower = player.Creature?.GetPower<ChordBonusPower>();
            bool shouldConsumeChordBonus =
                chordBonusPower != null && chordBonusPower.Amount > 0 && !state.ChordBonusConsumedThisOperation;
            if (shouldConsumeChordBonus)
                state.ChordBonusConsumedThisOperation = true;

            int firstPlayBonus = GetFirstPlayBonus(player);
            int fixedFirstPlayBonus = state.FirstPlayBonusAppliedThisOperation ? firstPlayBonus : 0;

            for (var i = 0; i < count; i++)
            {
                _ = ChordEffectPlayer.PlayChordIcons(player.Creature, new[] { chordId }, 0f);
                if (ChordManager.AllChords.TryGetValue(chordId, out var def))
                {
                    int baseBonus = CalculateBaseBonus(player);
                    int totalBonus = baseBonus + fixedFirstPlayBonus;
                    await def.Effect(context, player.Creature, totalBonus);
                }

                if (removeStored)
                    state.StoredChords.Remove(chordId);

                await NotifyChordPlayedAsync(player, context, state);
            }

            if (shouldConsumeChordBonus && chordBonusPower != null)
            {
                await PowerCmd.Decrement(chordBonusPower);
                PlayerNotesChanged?.Invoke(player);
            }
        }

        private static async Task NotifyChordPlayedAsync(Player player, PlayerChoiceContext context, StateData state)
        {
            foreach (var power in player.Creature.Powers.OfType<UnforgettablePerformancePower>())
                if (power.OnChordPlayed() && power.Amount > 0)
                    await PlayerCmd.GainEnergy(power.Amount, player);

            if (state.CurtainCallRecalledThisTurn) return;
            state.CurtainCallRecalledThisTurn = true;

            var curtainCallId = ModelDb.Card<CurtainCall>().Id.Entry;
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

            if (cardsToMove.Count > 0) GetGuitar(player)?.Flash();
        }

        // ==================== 公开演奏 API ====================

        public static async Task PlayChordAsync(Player player, string chordId, PlayerChoiceContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            var state = GetState(player);
            if (!state.IsActive) return;
            BeginPlayOperation(state, player);
            await PlaySingleChordInternalAsync(player, context, state, chordId, 1, removeStored: false);
            SaveState(player, state);
        }

        public static async Task PlayAllStoredChordsAsync(Player player, PlayerChoiceContext context, int countPerChord = 1, bool keepStored = false)
        {
            var state = GetState(player);
            if (!state.IsActive || state.StoredChords.Count == 0) return;
            BeginPlayOperation(state, player);
            var chords = state.StoredChords.ToList();
            if (!keepStored) state.StoredChords.Clear();
            foreach (var chordId in chords)
            {
                for (int i = 0; i < countPerChord; i++)
                    await PlaySingleChordInternalAsync(player, context, state, chordId, 1, removeStored: false);
            }
            SaveState(player, state);
            PlayerNotesChanged?.Invoke(player);
        }

        public static async Task PlayLastStoredChordAsync(Player player, PlayerChoiceContext context, int count = 1)
        {
            var state = GetState(player);
            if (!state.IsActive || state.StoredChords.Count == 0) return;
            BeginPlayOperation(state, player);
            var last = state.StoredChords.Last();
            for (int i = 0; i < count; i++)
                await PlaySingleChordInternalAsync(player, context, state, last, 1, removeStored: false);
            SaveState(player, state);
        }

        public static async Task PlayAllEquippedChordsAsync(Player player, PlayerChoiceContext context, int countPerChord = 1)
        {
            var state = GetState(player);
            if (!state.IsActive) return;
            BeginPlayOperation(state, player);
            var available = GetAvailableChordIds(player);
            foreach (var chordId in available)
                for (int i = 0; i < countPerChord; i++)
                    await PlaySingleChordInternalAsync(player, context, state, chordId, 1, removeStored: false);
            SaveState(player, state);
        }

        public static async Task PlayRandomEquippedChordAsync(Player player, PlayerChoiceContext context, int count = 1)
        {
            var state = GetState(player);
            if (!state.IsActive) return;
            var available = GetAvailableChordIds(player);
            if (available.Count == 0) return;
            BeginPlayOperation(state, player);
            var rng = player.RunState.Rng.CombatCardSelection;
            for (int i = 0; i < count; i++)
                await PlaySingleChordInternalAsync(player, context, state, rng.NextItem(available), 1, removeStored: false);
            SaveState(player, state);
        }
        
        /// <summary>
        /// 用于没有 PlayerChoiceContext 的钩子场景（如 AfterSideTurnStart）。
        /// 内部自建一个本地 HookPlayerChoiceContext。
        /// </summary>
        public static async Task PlayRandomEquippedChordImmediateAsync(Player player)
        {
            var state = GetState(player);
            if (!state.IsActive) return;
            var available = GetAvailableChordIds(player);
            if (available.Count == 0) return;

            BeginPlayOperation(state, player);
            var rng = player.RunState.Rng.CombatCardSelection;
            var randomChord = rng.NextItem(available);

            var ctx = new HookPlayerChoiceContext(player, player.NetId, GameActionType.Combat);
            var task = PlaySingleChordInternalAsync(player, ctx, state, randomChord, 1, removeStored: false);
            await ctx.AssignTaskAndWaitForPauseOrCompletion(task);
            SaveState(player, state);
        }

        // ==================== 手动添加存储和弦 ====================

        public static async Task AddStoredChordAsync(Player player, string chordId, int count = 1, PlayerChoiceContext? context = null)
        {
            var state = GetState(player);
            if (!state.IsActive || player.Creature?.CombatState == null) return;
            var combat = player.Creature.CombatState;
            var hasLingeringTaste = player.Creature.HasPower<LingeringTastePower>();

            // 重置操作标记（与 AddChordToStored 一致）
            state.FirstPlayBonusAppliedThisOperation = false;
            state.ChordBonusConsumedThisOperation = false;

            for (int i = 0; i < count; i++)
            {
                await ChordNoteHooks.BeforeChordMatched(combat, player, chordId, context);
                state.StoredChords.Add(chordId);
                await ChordNoteHooks.AfterChordMatched(combat, player, chordId, context);

                while (state.StoredChords.Count > MaxStoredChords)
                {
                    var overflow = state.StoredChords[0];
                    state.StoredChords.RemoveAt(0);
                    if (hasLingeringTaste && context != null)
                    {
                        TryConsumeFirstPlayBonus(state, player);
                        await PlaySingleChordInternalAsync(player, context, state, overflow, 1, removeStored: false);
                    }
                }
            }
            SaveState(player, state);
            PlayerNotesChanged?.Invoke(player);
        }

        // ==================== 查询方法 ====================

        public static int ClearNotesAndGetCount(Player player)
        {
            var state = GetState(player);
            int count = state.Notes.Count;
            state.Notes.Clear();
            SaveState(player, state);
            PlayerNotesChanged?.Invoke(player);
            return count;
        }

        public static void ClearNotes(Player player)
        {
            var state = GetState(player);
            state.Notes.Clear();
            SaveState(player, state);
            PlayerNotesChanged?.Invoke(player);
        }

        public static IReadOnlyList<CardType> GetCurrentNotes(Player player)
            => GetState(player).Notes.ToList().AsReadOnly();

        public static int GetNotesGainedThisTurn(Player player)
        {
            var state = GetState(player);
            var combat = player.Creature?.CombatState;
            if (combat != null && state.LastRoundNumber != combat.RoundNumber)
            {
                state.LastRoundNumber = combat.RoundNumber;
                state.NotesGainedThisTurn = 0;
                SaveState(player, state);
            }
            return state.NotesGainedThisTurn;
        }

        public static int GetTotalNotesThisCombat(Player player)
            => GetState(player).TotalNotesThisCombat;

        public static IReadOnlyList<string> GetStoredChords(Player player)
            => GetState(player).StoredChords.AsReadOnly();

        public static CardType? GetLastNoteType(Player player)
        {
            var notes = GetState(player).Notes;
            return notes.Count > 0 ? notes.Last() : (CardType?)null;
        }

        public static bool ModifyAllNotes(Player player, CardType newType)
        {
            var state = GetState(player);
            if (state.Notes.Count == 0) return false;
            var count = state.Notes.Count;
            state.Notes.Clear();
            for (int i = 0; i < count; i++) state.Notes.Enqueue(newType);
            SaveState(player, state);
            PlayerNotesChanged?.Invoke(player);
            return true;
        }

        public static bool ModifyLastNote(Player player, CardType newType)
        {
            var state = GetState(player);
            if (state.Notes.Count == 0) return false;
            var arr = state.Notes.ToArray();
            arr[^1] = newType;
            state.Notes.Clear();
            foreach (var n in arr) state.Notes.Enqueue(n);
            SaveState(player, state);
            PlayerNotesChanged?.Invoke(player);
            return true;
        }

        // ==================== 生命周期 ====================

        /// <summary>
        /// 在玩家回合开始时调用，重置本回合的首次演奏和 CurtainCall 标记。
        /// 应从 AnonGuitar.AfterPlayerTurnStart 中调用。
        /// </summary>
        public static void OnPlayerTurnStart(Player player)
        {
            var state = GetState(player);
            state.CurtainCallRecalledThisTurn = false;
            state.FirstPlayBonusConsumedThisTurn = false;
            state.FirstPlayBonusAppliedThisOperation = false;
            state.ChordBonusConsumedThisOperation = false;
            SaveState(player, state);
        }

        public static void OnCombatEnd(Player player)
        {
            var state = GetState(player);
            state.Notes.Clear();
            state.StoredChords.Clear();
            state.LastRoundNumber = 0;
            state.NotesGainedThisTurn = 0;
            state.TotalNotesThisCombat = 0;
            state.FirstPlayBonusConsumedThisTurn = false;
            state.FirstPlayBonusAppliedThisOperation = false;
            state.ChordBonusConsumedThisOperation = false;
            state.CurtainCallRecalledThisTurn = false;
            SaveState(player, state);
            ChordNoteUIManager.CleanupUI(player);
            PlayerNotesChanged?.Invoke(player);
        }
    }
}