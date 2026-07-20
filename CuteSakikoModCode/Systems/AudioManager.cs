using Godot;
using System.Security.Cryptography;
using System.Text;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Audio;
using CuteSakikoMod.CuteSakikoModCode.Others;
using System.Collections.Concurrent;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

/// <summary>
/// 管理模组的音效和音乐播放，基于 RitsuLib 的 FMOD 封装，增加预加载检查防止闪退。
/// </summary>
public static class AudioManager
{
    // ---- 音乐相关 ----
    private static AudioMusicHandle? _currentMusicHandle;
    private static string? _currentMusicPath;
    private static bool _nativeMusicStopped;

    // ---- 音效缓存（防止 GC 过早回收） ----
    private static readonly List<AudioFileHandle> _activeSoundHandles = new();
    private static readonly object _handleLock = new();
    private const int MaxCachedSoundHandles = 64;

    // ---- 路径编码修复缓存（映射原始路径 -> 纯 ASCII 临时路径） ----
    private static readonly Dictionary<string, string> _cachedAsciiPaths = new();
    private static readonly object _pathCacheLock = new();

    // ---- 预加载记录（避免重复尝试已知失败文件） ----
    private static readonly HashSet<string> _preloadedPaths = new();
    private static readonly object _preloadLock = new();

    // ---- 文件写入锁，防止并发复制同一文件 ----
    private static readonly ConcurrentDictionary<string, object> _fileWriteLocks = new();

    // ==================== 公开接口 ====================

    /// <summary>
    /// 播放一个短音效（非循环）。线程安全。
    /// </summary>
    public static void PlaySound(string filePath, float baseVolume = 1.0f)
    {
        if (!ModConfig.EnableAudio)
            return;

        if (IsMainThread())
            PlaySoundInternal(filePath, baseVolume);
        else
            Callable.From(() => PlaySoundInternal(filePath, baseVolume)).CallDeferred();
    }

    /// <summary>
    /// 播放背景音乐（循环）。线程安全，但非主线程调用会返回 null。
    /// </summary>
    public static AudioMusicHandle? PlayMusic(string filePath, float baseVolume = 1.0f)
    {
        if (!ModConfig.EnableAudio)
            return null;

        if (IsMainThread())
            return PlayMusicInternal(filePath, baseVolume);

        Callable.From(() => PlayMusicInternal(filePath, baseVolume)).CallDeferred();
        return null;
    }

    /// <summary>
    /// 刷新当前播放音乐的音量（用于设置变化后）。线程安全。
    /// </summary>
    public static void RefreshMusicVolume()
    {
        if (IsMainThread())
            RefreshMusicVolumeInternal();
        else
            Callable.From(RefreshMusicVolumeInternal).CallDeferred();
    }

    /// <summary>
    /// 停止当前播放的模组音乐，并恢复原生 BGM。线程安全。
    /// </summary>
    public static void StopMusic()
    {
        if (IsMainThread())
            StopMusicInternal();
        else
            Callable.From(StopMusicInternal).CallDeferred();
    }

    // ==================== 内部实现 ====================

    private static bool IsMainThread() => OS.GetMainThreadId() == OS.GetThreadCallerId();

    // ---- 路径编码修复 ----
    private static string? GetAsciiSafePath(string originalPath)
    {
        lock (_pathCacheLock)
        {
            if (_cachedAsciiPaths.TryGetValue(originalPath, out var cached))
                return cached;
        }

        // 检查源文件是否存在
        bool sourceExists;
        if (originalPath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
            sourceExists = Godot.FileAccess.FileExists(originalPath);
        else
            sourceExists = File.Exists(originalPath);

        if (!sourceExists)
        {
            STS2RitsuLib.RitsuLibFramework.Logger.Error($"[AudioManager] Audio file not found: {originalPath}");
            return null;
        }

        // 生成纯 ASCII 文件名
        byte[] hashBytes;
        using (var md5 = MD5.Create())
        {
            hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(originalPath));
        }
        var hashName = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        var extension = Path.GetExtension(originalPath);
        if (string.IsNullOrEmpty(extension))
            extension = ".mp3";

        // 关键修改：使用 OS.GetUserDataDir() 确保临时目录路径纯 ASCII
        var tempDir = Path.Combine(OS.GetUserDataDir(), "CuteSakikoModAudio");
        Directory.CreateDirectory(tempDir);

        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var safePath = Path.Combine(tempDir, $"{hashName}_{uniqueId}{extension}");

        if (!File.Exists(safePath))
        {
            // 防止并发写同一个 safePath（虽然 uniqueId 已防重，仍加锁保护）
            var writeLock = _fileWriteLocks.GetOrAdd(safePath, _ => new object());
            lock (writeLock)
            {
                if (!File.Exists(safePath))
                {
                    try
                    {
                        byte[] data;
                        if (originalPath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
                        {
                            using var file = Godot.FileAccess.Open(originalPath, Godot.FileAccess.ModeFlags.Read);
                            if (file == null)
                            {
                                STS2RitsuLib.RitsuLibFramework.Logger.Error($"[AudioManager] Failed to open Godot resource: {originalPath}");
                                return null;
                            }
                            var length = (long)file.GetLength();
                            data = file.GetBuffer(length);
                        }
                        else
                        {
                            data = File.ReadAllBytes(originalPath);
                        }

                        File.WriteAllBytes(safePath, data);
                    }
                    catch (Exception ex)
                    {
                        STS2RitsuLib.RitsuLibFramework.Logger.Error($"[AudioManager] Failed to copy audio file to safe path: {originalPath} -> {safePath}. Reason: {ex.Message}");
                        return null;
                    }
                }
            }
        }

        lock (_pathCacheLock)
        {
            _cachedAsciiPaths[originalPath] = safePath;
        }
        return safePath;
    }

    // ---- 预加载检查（使用 RitsuLib 的预加载接口） ----
    private static bool EnsurePreloadedAsSound(string safePath)
    {
        lock (_preloadLock)
        {
            if (_preloadedPaths.Contains(safePath))
                return true;
        }

        bool success = FmodStudioStreamingFiles.TryPreloadAsSound(safePath);

        if (success)
        {
            lock (_preloadLock)
            {
                _preloadedPaths.Add(safePath);
            }
        }
        else
        {
            STS2RitsuLib.RitsuLibFramework.Logger.Warn($"[AudioManager] FMOD cannot preload file, skipping playback: {safePath}");
        }

        return success;
    }

    private static bool EnsurePreloadedAsStreamingMusic(string safePath)
    {
        lock (_preloadLock)
        {
            if (_preloadedPaths.Contains(safePath))
                return true;
        }

        bool success = FmodStudioStreamingFiles.TryPreloadAsStreamingMusic(safePath);

        if (success)
        {
            lock (_preloadLock)
            {
                _preloadedPaths.Add(safePath);
            }
        }
        else
        {
            STS2RitsuLib.RitsuLibFramework.Logger.Warn($"[AudioManager] FMOD cannot preload music file, skipping playback: {safePath}");
        }

        return success;
    }

    // ---- 音效播放（内部） ----
    private static void PlaySoundInternal(string filePath, float baseVolume)
    {
        try
        {
            var safePath = GetAsciiSafePath(filePath);
            if (string.IsNullOrEmpty(safePath))
                return;

            // 🔒 预加载检查：FMOD 无法加载的文件直接跳过，避免原生层崩溃
            if (!EnsurePreloadedAsSound(safePath))
                return;

            var settings = SaveManager.Instance?.SettingsSave;
            var masterVol = settings?.VolumeMaster ?? 1.0f;
            var sfxVol = settings?.VolumeSfx ?? 1.0f;
            var modSfxVol = ModConfig.ModSfxVolume;

            var rawVol = baseVolume * masterVol * sfxVol * modSfxVol;
            if (float.IsNaN(rawVol) || float.IsInfinity(rawVol))
                rawVol = 1.0f;
            var finalVol = Mathf.Clamp(rawVol, 0.0f, 1.0f);

            // 定期清理无效句柄
            PurgeInvalidHandles();

            var handle = FmodStudioStreamingFiles.TryCreateSoundHandle(safePath);
            if (handle != null)
            {
                if (GodotObject.IsInstanceValid(handle.RawInstance))
                {
                    handle.RawInstance.Call("set_volume", finalVol);
                    handle.RawInstance.Call("play");
                    CacheHandle(handle);
                }
                else
                {
                    // 句柄无效，回退到简单播放
                    FmodStudioStreamingFiles.TryPlaySoundFile(safePath, finalVol);
                }
            }
            else
            {
                // 无法创建句柄，回退到简单播放
                FmodStudioStreamingFiles.TryPlaySoundFile(safePath, finalVol);
            }
        }
        catch (Exception e)
        {
            STS2RitsuLib.RitsuLibFramework.Logger.Error($"[AudioManager] PlaySound error: {e}");
        }
    }

    private static void CacheHandle(AudioFileHandle handle)
    {
        lock (_handleLock)
        {
            _activeSoundHandles.Add(handle);
            while (_activeSoundHandles.Count > MaxCachedSoundHandles)
            {
                var oldest = _activeSoundHandles[0];
                if (GodotObject.IsInstanceValid(oldest.RawInstance))
                    oldest.RawInstance.Call("stop");
                _activeSoundHandles.RemoveAt(0);
            }
        }
    }

    private static void PurgeInvalidHandles()
    {
        lock (_handleLock)
        {
            _activeSoundHandles.RemoveAll(h => !GodotObject.IsInstanceValid(h.RawInstance));
        }
    }

    // ---- 音乐播放（内部） ----
    private static AudioMusicHandle? PlayMusicInternal(string filePath, float baseVolume)
    {
        try
        {
            var safePath = GetAsciiSafePath(filePath);
            if (string.IsNullOrEmpty(safePath))
                return null;

            // 🔒 预加载检查
            if (!EnsurePreloadedAsStreamingMusic(safePath))
                return null;

            if (_currentMusicHandle != null &&
                GodotObject.IsInstanceValid(_currentMusicHandle.RawInstance) &&
                _currentMusicPath == safePath)
            {
                return _currentMusicHandle;
            }

            StopMusicInternal();

            if (!_nativeMusicStopped)
            {
                NRunMusicController.Instance?.StopMusic();
                _nativeMusicStopped = true;
            }

            var settings = SaveManager.Instance?.SettingsSave;
            var masterVol = settings?.VolumeMaster ?? 1.0f;
            var bgmVol = settings?.VolumeBgm ?? 1.0f;
            var modBgmVol = ModConfig.ModBgmVolume;

            var rawVol = baseVolume * masterVol * modBgmVol * bgmVol;
            if (float.IsNaN(rawVol) || float.IsInfinity(rawVol))
                rawVol = 1.0f;
            var finalVol = Mathf.Clamp(rawVol, 0.0f, 1.0f);

            var options = new AudioPlaybackOptions { Scope = AudioLifecycleScope.Combat };
            var handle = FmodStudioStreamingFiles.TryCreateStreamingMusicHandle(safePath, options);
            if (handle != null && GodotObject.IsInstanceValid(handle.RawInstance))
            {
                handle.RawInstance.Call("set_volume", finalVol);
                handle.RawInstance.Call("play");
                handle.RawInstance.Call("set_loop_count", -1);
                _currentMusicHandle = handle;
                _currentMusicPath = safePath;
            }
            return handle;
        }
        catch (Exception e)
        {
            STS2RitsuLib.RitsuLibFramework.Logger.Error($"[AudioManager] PlayMusic error: {e}");
            return null;
        }
    }

    // ---- 刷新音乐音量（内部） ----
    private static void RefreshMusicVolumeInternal()
    {
        try
        {
            if (_currentMusicHandle == null || !GodotObject.IsInstanceValid(_currentMusicHandle.RawInstance))
            {
                _currentMusicHandle = null;
                _currentMusicPath = null;
                return;
            }

            var settings = SaveManager.Instance?.SettingsSave;
            var masterVol = settings?.VolumeMaster ?? 1.0f;
            var bgmVol = settings?.VolumeBgm ?? 1.0f;
            var modBgmVol = ModConfig.ModBgmVolume;
            var rawVol = 1.0f * masterVol * bgmVol * modBgmVol;
            if (float.IsNaN(rawVol) || float.IsInfinity(rawVol))
                rawVol = 1.0f;
            var finalVol = Mathf.Clamp(rawVol, 0.0f, 1.0f);
            _currentMusicHandle.RawInstance.Call("set_volume", finalVol);
        }
        catch (Exception e)
        {
            STS2RitsuLib.RitsuLibFramework.Logger.Error($"[AudioManager] RefreshMusicVolume error: {e}");
        }
    }

    // ---- 停止音乐（内部） ----
    private static void StopMusicInternal()
    {
        try
        {
            if (_currentMusicHandle != null)
            {
                if (GodotObject.IsInstanceValid(_currentMusicHandle.RawInstance))
                    _currentMusicHandle.RawInstance.Call("stop");
                _currentMusicHandle = null;
                _currentMusicPath = null;
            }

            if (_nativeMusicStopped)
            {
                NRunMusicController.Instance?.UpdateMusic();
                _nativeMusicStopped = false;
            }
        }
        catch (Exception e)
        {
            STS2RitsuLib.RitsuLibFramework.Logger.Error($"[AudioManager] StopMusic error: {e}");
        }
    }
}