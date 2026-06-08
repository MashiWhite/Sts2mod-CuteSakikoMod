// 扩展方法：按类型查找子节点（递归）

using Godot;

namespace CuteSakikoMod.CuteSakikoModCode.Others;

public static class NodeExtensions
{
    public static T? FindChildOfType<T>(this Node parent) where T : class
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is T t) return t;
            var found = child.FindChildOfType<T>();
            if (found != null) return found;
        }
        return null;
    }
    public static IEnumerable<T> FindChildrenOfType<T>(this Node parent) where T : class
    {
        foreach (var child in parent.GetChildren())
        {
            if (child is T t) yield return t;
            foreach (var found in child.FindChildrenOfType<T>())
                yield return found;
        }
    }
}