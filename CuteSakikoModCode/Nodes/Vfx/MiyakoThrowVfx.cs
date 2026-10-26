using Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Nodes.Vfx;

/// <summary>
///     藤都子攻击时随手扔出去的东西：月饼 / 壶 / 鸭子，每次随机只显示一个。
///     只做表现层随机（用 Godot 的全局 RNG），不影响战斗 RNG 的同步。
///     投掷轨迹由 Miyako 的招式代码负责（和 Hotaru 扔包子一样的 tween）。
/// </summary>
public partial class MiyakoThrowVfx : Node2D
{
    private static readonly string[] SpriteNames = ["Mooncake", "Tsubo", "Duck"];

    /// <summary>本次随机选中的贴图（碎片要跟它保持一致）。</summary>
    public Texture2D? PickedTexture { get; private set; }

    public override void _Ready()
    {
        var sprites = new List<Sprite2D>();
        foreach (var name in SpriteNames)
        {
            var sprite = GetNodeOrNull<Sprite2D>(name);
            if (sprite != null) sprites.Add(sprite);
        }

        if (sprites.Count == 0) return;

        var pick = sprites[(int)(GD.Randi() % (uint)sprites.Count)];
        foreach (var sprite in sprites)
            sprite.Visible = ReferenceEquals(sprite, pick);

        PickedTexture = pick.Texture;

        // 随手扔的感觉：随机一点角度和大小
        pick.Rotation = (float)(GD.Randf() * 0.8 - 0.4);
        var scale = (float)(0.9 + GD.Randf() * 0.2);
        pick.Scale = new Vector2(scale, scale);
    }
}
