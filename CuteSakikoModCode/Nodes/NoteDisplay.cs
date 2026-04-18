using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using System.Collections.Generic;
using System.Linq;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes
{
    public partial class NoteDisplay : Control
    {
        private List<TextureRect> _iconSlots;
        private List<CardType?> _currentTypes; // 记录每个槽位当前显示的音符类型
        private Texture2D _attackTex;
        private Texture2D _skillTex;
        private Texture2D _powerTex;
        private Texture2D _specialTex;

        public override void _Ready()
        {
            _iconSlots = new List<TextureRect>
            {
                GetNode<TextureRect>("IconContainer/atkIcon"),
                GetNode<TextureRect>("IconContainer/sklIcon"),
                GetNode<TextureRect>("IconContainer/powIcon"),
                GetNode<TextureRect>("IconContainer/speIcon")
            };

            _currentTypes = new List<CardType?> { null, null, null, null };

            // 缓存纹理
            _attackTex = _iconSlots[0].Texture;
            _skillTex = _iconSlots[1].Texture;
            _powerTex = _iconSlots[2].Texture;
            _specialTex = _iconSlots[3].Texture;

            // 初始化：隐藏所有图标，并连接鼠标事件
            for (int i = 0; i < _iconSlots.Count; i++)
            {
                var slot = _iconSlots[i];
                slot.Visible = false;
                slot.MouseFilter = MouseFilterEnum.Stop;
                // 使用局部变量捕获索引，避免闭包问题
                int index = i;
                slot.MouseEntered += () => OnSlotMouseEntered(index);
                slot.MouseExited += () => OnSlotMouseExited(index);
            }
        }

        public void UpdateNotes(IEnumerable<CardType> notes)
        {
            // 先全部隐藏，并清除类型记录
            for (int i = 0; i < _iconSlots.Count; i++)
            {
                _iconSlots[i].Visible = false;
                _currentTypes[i] = null;
            }

            int index = 0;
            foreach (var type in notes.Take(4))
            {
                if (index >= _iconSlots.Count) break;
                var slot = _iconSlots[index];
                slot.Texture = GetIconForType(type);
                slot.Visible = true;
                _currentTypes[index] = type;
                index++;
            }
        }

        private Texture2D GetIconForType(CardType type)
        {
            return type switch
            {
                CardType.Attack => _attackTex,
                CardType.Skill => _skillTex,
                CardType.Power => _powerTex,
                _ => _specialTex
            };
        }

        private void OnSlotMouseEntered(int index)
        {
            var type = _currentTypes[index];
            if (type == null) return;

            var slot = _iconSlots[index];
            var tip = ChordDisplayHelper.GetNoteTypeHoverTip(type.Value);
            var alignment = HoverTip.GetHoverTipAlignment(slot, 0.5f);
            NHoverTipSet.CreateAndShow(slot, tip, alignment);
        }

        private void OnSlotMouseExited(int index)
        {
            var slot = _iconSlots[index];
            NHoverTipSet.Remove(slot);
        }

        public override void _ExitTree()
        {
            foreach (var slot in _iconSlots)
                NHoverTipSet.Remove(slot);
            base._ExitTree();
        }
    }
}