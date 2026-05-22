using System.Collections.Concurrent;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.TestSupport;

namespace CuteSakikoMod.CuteSakikoModCode.Others;

public static class VFXUtil
{
    public static readonly ConcurrentDictionary<string, PackedScene> ModSceneCache = new();

    /// <summary>预加载一批 VFX 场景（在 Entry.cs 中调用）</summary>
    public static void PreloadScenes(IEnumerable<string> scenePaths)
    {
        foreach (var path in scenePaths)
        {
            if (ModSceneCache.ContainsKey(path)) continue;
            var scene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Reuse);
            if (scene != null)
                ModSceneCache[path] = scene;
        }
    }

    /// <summary>从缓存或 PreloadManager 获取场景实例</summary>
    public static Node2D GenVFXNode(string scenePath)
    {
        if (ModSceneCache.TryGetValue(scenePath, out var modScene))
            return modScene.Instantiate<Node2D>();
        return PreloadManager.Cache.GetScene(scenePath).Instantiate<Node2D>();
    }

    public static T GenVFXNode<T>(string scenePath) where T : Node2D
    {
        if (ModSceneCache.TryGetValue(scenePath, out var modScene))
            return modScene.Instantiate<T>();
        return PreloadManager.Cache.GetScene(scenePath).Instantiate<T>();
    }

    /// <summary>简单播放前台特效，定时销毁</summary>
    public static Node2D? PlaySimple(string scenePath, Vector2 position, float lifetime = 2f)
    {
        if (TestMode.IsOn || NCombatRoom.Instance == null) return null;

        var node = GenVFXNode(scenePath);
        NCombatRoom.Instance.CombatVfxContainer.AddChildSafely(node);
        node.GlobalPosition = position;

        var timer = node.GetTree().CreateTimer(lifetime);
        timer.Timeout += () =>
        {
            if (GodotObject.IsInstanceValid(node))
                node.QueueFreeSafely();
        };
        return node;
    }
}