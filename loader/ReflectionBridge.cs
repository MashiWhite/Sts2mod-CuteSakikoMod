using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;

namespace CuteSakikoMod.Loader;

internal static class ReflectionBridge
{
    private static readonly List<Assembly> Assemblies = new();
    private static Type[]? _types;

    internal static void Register(Assembly assembly)
    {
        lock (Assemblies)
        {
            if (!Assemblies.Contains(assembly)) Assemblies.Add(assembly);
            _types = null;
        }
    }

    internal static void Install(Assembly loaderAssembly)
    {
        try
        {
            new Harmony("CuteSakikoMod.Loader.ReflectionBridge").PatchAll(loaderAssembly);
        }
        catch (Exception ex)
        {
            Log.Warn("[CuteSakikoMod.Loader] ReflectionBridge 安装失败：{ex.Message}");
        }
    }

    internal static Type[] GetTypes()
    {
        lock (Assemblies)
        {
            if (_types != null) return _types;
            var types = new List<Type>();
            foreach (var assembly in Assemblies)
            {
                try { types.AddRange(assembly.GetTypes()); }
                catch (ReflectionTypeLoadException ex) { types.AddRange(ex.Types.OfType<Type>()); }
            }
            _types = types.Distinct().ToArray();
            return _types;
        }
    }
}

[HarmonyPatch(typeof(ReflectionHelper), nameof(ReflectionHelper.ModTypes), MethodType.Getter)]
internal static class ReflectionHelperModTypesPatch
{
    private static void Postfix(ref Type[] __result)
    {
        var variantTypes = ReflectionBridge.GetTypes();
        if (variantTypes.Length == 0) return;
        var seen = new HashSet<Type>(__result);
        var additions = new List<Type>();
        foreach (var type in variantTypes)
            if (seen.Add(type)) additions.Add(type);
        if (additions.Count > 0)
            __result = __result.Concat(additions).ToArray();
    }
}