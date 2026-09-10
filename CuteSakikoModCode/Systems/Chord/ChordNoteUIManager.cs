using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using STS2RitsuLib.Utils;
using System.Collections.Generic;
using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.Nodes;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Chord
{
    /// <summary>
    /// 音符与储存和弦的 UI 管理器，独立于吉他遗物。
    /// 使用 AttachedState 存储每个玩家的 UI 节点引用。
    /// </summary>
    public static class ChordNoteUIManager
    {
        private sealed class UIData
        {
            public NoteDisplay NoteDisplay;
            public StoredChordDisplay StoredChordDisplay;
        }

        private static readonly AttachedState<Player, UIData> _uiData = new(() => new UIData());

        // 订阅音符系统事件，自动更新 UI
        static ChordNoteUIManager()
        {
            ChordNoteSystem.PlayerNotesChanged += OnPlayerNotesChanged;
        }

        private static void OnPlayerNotesChanged(Player player)
        {
            UpdateNoteDisplay(player);
            UpdateStoredChordDisplay(player);
        }

        /// <summary>
        /// 确保 UI 已创建并更新显示。
        /// </summary>
        public static void UpdateNoteDisplay(Player player)
        {
            if (player?.Creature?.CombatState == null) return;
            var data = _uiData[player];

            // 确保 UI 存在
            if (data.NoteDisplay == null || !GodotObject.IsInstanceValid(data.NoteDisplay))
            {
                EnsureNoteDisplay(player, data);
            }

            if (data.NoteDisplay != null && GodotObject.IsInstanceValid(data.NoteDisplay))
            {
                var notes = ChordNoteSystem.GetCurrentNotes(player);
                data.NoteDisplay.UpdateNotes(notes);
            }
        }

        public static void UpdateStoredChordDisplay(Player player)
        {
            if (player?.Creature?.CombatState == null) return;
            var data = _uiData[player];

            if (data.StoredChordDisplay == null || !GodotObject.IsInstanceValid(data.StoredChordDisplay))
            {
                EnsureStoredChordDisplay(player, data);
            }

            if (data.StoredChordDisplay != null && GodotObject.IsInstanceValid(data.StoredChordDisplay))
            {
                var chords = ChordNoteSystem.GetStoredChords(player).ToList();
                data.StoredChordDisplay.UpdateChords(chords, 0);
            }
        }

        private static void EnsureNoteDisplay(Player player, UIData data)
        {
            var creatureNode = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
            if (creatureNode == null) return;

            var scene = GD.Load<PackedScene>("res://CuteSakikoMod/scenes/ui/note_display.tscn");
            if (scene == null) return;

            var display = scene.Instantiate<Control>();
            creatureNode.AddChild(display);
            display.Position = new Vector2(-110, -383);
            data.NoteDisplay = display as NoteDisplay;
        }

        private static void EnsureStoredChordDisplay(Player player, UIData data)
        {
            var creatureNode = NCombatRoom.Instance?.GetCreatureNode(player.Creature);
            if (creatureNode == null) return;

            var scene = GD.Load<PackedScene>("res://CuteSakikoMod/scenes/ui/stored_chord_display.tscn");
            if (scene == null) return;

            var display = scene.Instantiate<Control>();
            creatureNode.AddChild(display);
            display.Position = new Vector2(-140, -230);
            data.StoredChordDisplay = display as StoredChordDisplay;
        }

        /// <summary>
        /// 清理指定玩家的 UI。
        /// </summary>
        public static void CleanupUI(Player player)
        {
            var data = _uiData[player];

            if (data.NoteDisplay != null && GodotObject.IsInstanceValid(data.NoteDisplay))
                data.NoteDisplay.QueueFree();
            data.NoteDisplay = null;

            if (data.StoredChordDisplay != null && GodotObject.IsInstanceValid(data.StoredChordDisplay))
                data.StoredChordDisplay.QueueFree();
            data.StoredChordDisplay = null;
        }

        /// <summary>
        /// 战斗结束时清理所有玩家 UI（由 ChordNoteSystem.OnCombatEnd 调用）。
        /// </summary>
        public static void CleanupAllForCombat()
        {
            // 由于 AttachedState 没有枚举所有 key 的方法，这里由外部在战斗结束时遍历玩家调用 CleanupUI。
            // 实际调用在 ChordNoteSystem.OnCombatEnd 中遍历所有玩家执行。
        }
    }
}