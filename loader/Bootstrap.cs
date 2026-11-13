using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text.Json;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace CuteSakikoMod.Loader;

/// <summary>
/// CuteSakikoMod 的版本加载器。
///
/// 游戏只会加载 mod 目录里的 <c>CuteSakikoMod.dll</c>（0.113 ModManager.cs:799），
/// 所以真正按游戏版本区分的实现放在 <c>compat/&lt;版本&gt;/CuteSakikoMod.Compat.dll</c>，
/// 由本加载器读清单后选最高且不高于当前游戏版本的变体加载，并关联回 Mod 对象，
/// 再反射调用变体里的 Entry.Init。
///
/// 结构照抄工坊 RitsuLib（3747602295）的 loader + compat/&lt;target&gt; + variants.manifest。
/// </summary>
[ModInitializer(nameof(Initialize))]
public static class Bootstrap
{
    private const string ModId = "CuteSakikoMod";
    private const string ManifestName = "csk-variants.manifest";
    private const string EntryTypeName = "CuteSakikoMod.Entry";
    private const string EntryMethodName = "Init";

    private static int _state;

    public static void Initialize()
    {
        if (Interlocked.CompareExchange(ref _state, 1, 0) != 0)
            return;

        try
        {
            var loaderAssembly = typeof(Bootstrap).Assembly;
            ReflectionBridge.Install(loaderAssembly);
            var loaderDir = Path.GetDirectoryName(loaderAssembly.Location)
                            ?? throw new InvalidOperationException("找不到加载器所在目录");

            var hostVersion = DetectHostVersion(loaderDir);
            var layout = ReadLayout(loaderDir, hostVersion);
            Log.Info($"[CuteSakikoMod.Loader] 游戏版本={hostVersion?.ToString() ?? "未知"}；" +
                     $"选中变体={layout.CompatTarget}");

            var context = AssemblyLoadContext.GetLoadContext(loaderAssembly) ?? AssemblyLoadContext.Default;
            var variant = LoadVariant(context, layout);
            ReflectionBridge.Register(variant);

            AssociateWithMod(variant);

            var entry = variant.GetType(EntryTypeName, throwOnError: true)
                        ?? throw new TypeLoadException(EntryTypeName);
            var method = entry.GetMethod(EntryMethodName, BindingFlags.Public | BindingFlags.Static)
                         ?? throw new MissingMethodException(EntryTypeName, EntryMethodName);
            method.Invoke(null, null);

            Volatile.Write(ref _state, 2);
        }
        catch (Exception ex)
        {
            Volatile.Write(ref _state, -1);
            // 不向上抛：0.113 里初始化异常会被游戏记成 mod 初始化失败；
            // 这里只让本 mod 失效，不影响其它 mod 与游戏启动。
            Log.Error($"[CuteSakikoMod.Loader] 初始化失败：{ex}");
        }
    }

    private sealed record Variant(string CompatTarget, string AssemblyName, string Path, string Sha256);

    private static Variant ReadLayout(string root, Version? host)
    {
        var manifestPath = Path.Combine(root, ManifestName);
        using var stream = File.OpenRead(manifestPath);
        if (stream.Length > 1024 * 1024)
            throw new InvalidDataException("变体清单超过 1 MiB");

        using var document = JsonDocument.Parse(stream, new JsonDocumentOptions { MaxDepth = 8 });
        var schema = document.RootElement.GetProperty("schema").GetInt32();
        if (schema != 1)
            throw new InvalidDataException($"不支持的变体清单 schema：{schema}");

        Variant? selected = null;
        Version? selectedVersion = null;
        foreach (var item in document.RootElement.GetProperty("variants").EnumerateArray())
        {
            var targetText = item.GetProperty("compatTarget").GetString();
            if (!Version.TryParse(targetText, out var targetVersion))
                throw new InvalidDataException($"无效的 compatTarget：{targetText}");
            if (host != null && targetVersion > host)
                continue;

            var assemblyName = item.GetProperty("assembly").GetString()
                               ?? throw new InvalidDataException("变体缺少 assembly 字段");
            var sha = item.GetProperty("sha256").GetString()
                      ?? throw new InvalidDataException("变体缺少 sha256 字段");
            if (selectedVersion != null && targetVersion <= selectedVersion)
                continue;

            selectedVersion = targetVersion;
            selected = new Variant(targetText!, assemblyName,
                Path.Combine(root, "compat", targetText!, assemblyName + ".dll"), sha);
        }

        return selected ?? throw new NotSupportedException(
            $"这个海克斯包不支持当前游戏版本 {host?.ToString() ?? "未知"}");
    }

    private static Assembly LoadVariant(AssemblyLoadContext context, Variant layout)
    {
        if (!File.Exists(layout.Path))
            throw new FileNotFoundException("变体程序集不存在", layout.Path);

        using (var stream = File.OpenRead(layout.Path))
        {
            var actual = Convert.ToHexString(SHA256.HashData(stream));
            if (!string.Equals(actual, layout.Sha256, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"变体哈希不匹配：{layout.Path}");
        }

        var existing = context.Assemblies.FirstOrDefault(assembly =>
            string.Equals(assembly.GetName().Name, layout.AssemblyName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            return existing;

        return context.LoadFromAssemblyPath(layout.Path);
    }

    private static void AssociateWithMod(Assembly variant)
    {
        // 0.107 的 ModManager 没有 AssociateAssemblyWithMod，必须反射探测后再退回 Mod.assemblies。
        try
        {
            var method = typeof(ModManager).GetMethod(
                "AssociateAssemblyWithMod",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(string), typeof(Assembly) },
                modifiers: null);
            if (method != null)
            {
                method.Invoke(null, new object[] { ModId, variant });
                return;
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"[CuteSakikoMod.Loader] AssociateAssemblyWithMod 失败：{ex.Message}");
        }

        // 兜底：直接挂到 Mod.assemblies（0.107 / 0.111 / 0.113 的 Mod 都有这个字段）。
        foreach (var mod in ModManager.Mods)
        {
            var manifest = mod.GetType().GetField("manifest",
                BindingFlags.Public | BindingFlags.Instance)?.GetValue(mod) as ModManifest;
            if (!string.Equals(manifest?.id, ModId, StringComparison.Ordinal))
                continue;

            var assembliesField = mod.GetType().GetField("assemblies",
                BindingFlags.Public | BindingFlags.Instance);
            if (assembliesField?.GetValue(mod) is System.Collections.IList assemblies)
            {
                if (!assemblies.Contains(variant))
                    assemblies.Add(variant);
                return;
            }

            // 0.107 的 Mod 只有一个 assembly 字段，没有列表。
            var singleField = mod.GetType().GetField("assembly",
                BindingFlags.Public | BindingFlags.Instance);
            if (singleField != null && singleField.FieldType.IsAssignableFrom(variant.GetType()))
            {
                singleField.SetValue(mod, variant);
                Log.Info($"[{ModId}.Loader] 变体已写入 Mod.assembly（0.107 单字段路径）。");
            }
            else
            {
                Log.Warn($"[{ModId}.Loader] 无法关联变体：Mod 既没有 assemblies 列表，也没有可写的 assembly 字段。");
            }
            return;
        }
    }

    /// <summary>
    /// 取游戏发布版本。顺序：release_info.json（exe 目录 / 启动器缓存 / loader 与 sts2 附近）
    /// → 游戏自己的 ReleaseInfoManager。**不能**用程序集信息版本：sts2 的产品版本是 0.1.0，
    /// 不是发布版本（2026-11-13 实机踩坑）。
    /// </summary>
    private static Version? DetectHostVersion(string loaderDir)
    {
        foreach (var candidate in GetReleaseInfoCandidates(loaderDir))
        {
            if (TryReadReleaseInfo(candidate, out var version))
                return Normalize(version);
        }

        return TryReadVersionFromGameApi();
    }

    private static List<string> GetReleaseInfoCandidates(string loaderDir)
    {
        var list = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(string? path)
        {
            if (!string.IsNullOrEmpty(path) && seen.Add(path))
                list.Add(path);
        }

        void AddTree(string? root, int depth)
        {
            if (string.IsNullOrEmpty(root))
                return;
            var dir = new DirectoryInfo(root);
            for (var i = 0; i < depth && dir != null; i++)
            {
                Add(Path.Combine(dir.FullName, "release_info.json"));
                dir = dir.Parent;
            }
        }

        var executable = TryGetGodotString("GetExecutablePath");
        if (!string.IsNullOrEmpty(executable))
        {
            var executableDir = Path.GetDirectoryName(executable);
            AddTree(executableDir, 2);
            if (string.Equals(TryGetGodotString("GetName"), "macOS", StringComparison.Ordinal) &&
                !string.IsNullOrEmpty(executableDir))
            {
                Add(Path.Combine(executableDir, "..", "Resources", "release_info.json"));
            }
        }

        var dataDir = TryGetGodotString("GetDataDir");
        if (!string.IsNullOrEmpty(dataDir))
            Add(Path.Combine(dataDir, "game", "release_info.json"));

        AddTree(loaderDir, 4);

        try
        {
            AddTree(Path.GetDirectoryName(typeof(ModInitializerAttribute).Assembly.Location), 3);
        }
        catch
        {
            // 取不到 sts2 路径就只靠上面的候选。
        }

        return list;
    }

    private static bool TryReadReleaseInfo(string path, out Version version)
    {
        version = default!;
        try
        {
            if (!File.Exists(path))
                return false;
            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);
            if (!document.RootElement.TryGetProperty("version", out var element))
                return false;
            var text = element.GetString();
            return text != null && TryParseReleaseVersion(text, out version);
        }
        catch
        {
            return false;
        }
    }

    private static Version? TryReadVersionFromGameApi()
    {
        try
        {
            var manager = Type.GetType("MegaCrit.Sts2.Core.Debug.ReleaseInfoManager, sts2", throwOnError: false);
            var instance = manager?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
                ?.GetValue(null);
            var info = instance?.GetType().GetProperty("ReleaseInfo")?.GetValue(instance);
            var text = info?.GetType().GetProperty("Version")?.GetValue(info) as string;
            if (text != null && TryParseReleaseVersion(text, out var version))
                return Normalize(version);
        }
        catch
        {
            // 反射失败就当未知版本。
        }

        return null;
    }

    private static string? TryGetGodotString(string methodName)
    {
        try
        {
            var osType = Type.GetType("Godot.OS, GodotSharp", throwOnError: false)
                         ?? AppDomain.CurrentDomain.GetAssemblies()
                             .Select(static assembly => assembly.GetType("Godot.OS", throwOnError: false))
                             .FirstOrDefault(static type => type != null);
            var method = osType?.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
            return method?.Invoke(null, null) as string;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryParseReleaseVersion(string text, out Version version)
    {
        var value = text.Trim();
        var dash = value.IndexOf('-', StringComparison.Ordinal);
        if (dash >= 0)
            value = value[..dash].Trim();
        var plus = value.IndexOf('+', StringComparison.Ordinal);
        if (plus >= 0)
            value = value[..plus].Trim();
        if (value.Length >= 2 && (value[0] == 'v' || value[0] == 'V') && char.IsDigit(value[1]))
            value = value[1..];
        return Version.TryParse(value, out version);
    }

    private static Version Normalize(Version version) =>
        version.Build < 0 ? new Version(version.Major, version.Minor, 0) : version;
}
