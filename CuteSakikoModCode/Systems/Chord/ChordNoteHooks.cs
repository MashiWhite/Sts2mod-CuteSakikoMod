using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Threading.Tasks;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Chord
{
    /// <summary>
    /// 音符/和弦事件的官方风格 Hook 调度器。
    /// </summary>
    public static class ChordNoteHooks
    {
        public static async Task BeforeNoteAdded(ICombatState combat, Player player, CardType noteType, PlayerChoiceContext? context)
        {
            if (combat == null || player == null) return;
            foreach (var model in combat.IterateHookListeners())
            {
                try
                {
                    if (model is IChordNoteHookHandler handler)
                        await handler.BeforeNoteAdded(player, noteType, context);
                }
                finally
                {
                    model.InvokeExecutionFinished();
                }
            }
        }

        public static async Task AfterNoteAdded(ICombatState combat, Player player, CardType noteType, PlayerChoiceContext? context)
        {
            if (combat == null || player == null) return;
            foreach (var model in combat.IterateHookListeners())
            {
                try
                {
                    if (model is IChordNoteHookHandler handler)
                        await handler.AfterNoteAdded(player, noteType, context);
                }
                finally
                {
                    model.InvokeExecutionFinished();
                }
            }
        }

        public static async Task BeforeChordMatched(ICombatState combat, Player player, string chordId, PlayerChoiceContext? context)
        {
            if (combat == null || player == null) return;
            foreach (var model in combat.IterateHookListeners())
            {
                try
                {
                    if (model is IChordNoteHookHandler handler)
                        await handler.BeforeChordMatched(player, chordId, context);
                }
                finally
                {
                    model.InvokeExecutionFinished();
                }
            }
        }

        public static async Task AfterChordMatched(ICombatState combat, Player player, string chordId, PlayerChoiceContext? context)
        {
            if (combat == null || player == null) return;
            foreach (var model in combat.IterateHookListeners())
            {
                try
                {
                    if (model is IChordNoteHookHandler handler)
                        await handler.AfterChordMatched(player, chordId, context);
                }
                finally
                {
                    model.InvokeExecutionFinished();
                }
            }
        }

        public static async Task BeforeChordPlayed(ICombatState combat, Player player, string chordId, int bonus, PlayerChoiceContext? context)
        {
            if (combat == null || player == null) return;
            foreach (var model in combat.IterateHookListeners())
            {
                try
                {
                    if (model is IChordNoteHookHandler handler)
                        await handler.BeforeChordPlayed(player, chordId, bonus, context);
                }
                finally
                {
                    model.InvokeExecutionFinished();
                }
            }
        }

        public static async Task AfterChordPlayed(ICombatState combat, Player player, string chordId, int bonus, PlayerChoiceContext? context)
        {
            if (combat == null || player == null) return;
            foreach (var model in combat.IterateHookListeners())
            {
                try
                {
                    if (model is IChordNoteHookHandler handler)
                        await handler.AfterChordPlayed(player, chordId, bonus, context);
                }
                finally
                {
                    model.InvokeExecutionFinished();
                }
            }
        }
    }
}