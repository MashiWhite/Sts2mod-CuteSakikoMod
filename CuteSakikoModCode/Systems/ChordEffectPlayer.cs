using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace CuteSakikoMod.CuteSakikoModCode.Systems
{
    public static class ChordEffectPlayer
    {
        /// <summary>
        /// 在角色上方依次播放多个和弦图标特效（中心放大 + 缓慢淡出）
        /// </summary>
        /// <param name="creature">目标角色</param>
        /// <param name="chordIds">和弦ID列表</param>
        /// <param name="interval">每个图标出现之间的间隔（秒）</param>
        public static async Task PlayChordIcons(Creature creature, IEnumerable<string> chordIds, float interval = 0.6f)
        {
            var combatState = creature.CombatState;
            if (combatState == null) return;

            var creatureNode = NCombatRoom.Instance?.GetCreatureNode(creature);
            if (creatureNode == null) return;

            foreach (var chordId in chordIds)
            {
                var texture = ChordDisplayHelper.GetChordTexture(chordId);
                if (texture == null) continue;

                // 创建图标
                var icon = new TextureRect
                {
                    Texture = texture,
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    Size = new Vector2(64, 64),
                    Position = new Vector2(-42, -220),        // 位置
                    Modulate = new Color(1, 1, 1, 0.8f),     // 初始不透明度80%
                    Scale = Vector2.One,
                    // 设置缩放锚点为图标中心
                    PivotOffset = new Vector2(32, 32)         // Size的一半
                };

                creatureNode.AddChild(icon);

                // 动画：1.5秒内从1倍放大到1.5倍，同时渐隐
                var tween = icon.CreateTween();
                tween.SetParallel();
                tween.TweenProperty(icon, "scale", new Vector2(1.5f, 1.5f), 1f);
                tween.TweenProperty(icon, "modulate:a", 0.0f, 1f);
                tween.SetParallel(false);

                // 动画结束后移除图标
                tween.TweenCallback(Callable.From(() =>
                {
                    if (GodotObject.IsInstanceValid(icon))
                        icon.QueueFree();
                }));

                // 等待间隔后播放下一个
                await Task.Delay((int)(interval * 1000));
            }
        }
    }
}