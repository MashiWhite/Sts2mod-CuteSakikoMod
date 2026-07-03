using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Audio;
using CuteSakikoMod.CuteSakikoModCode.Others;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

/// <summary>
/// 管理模组的音效和音乐播放，提供线程安全、GC 安全和路径编码安全封装。
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
    /// 播放背景音乐（循环）。线程安全，但非主线程调用会返回 null（调用者应处理）。
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
    /// <summary>
    /// 将任意路径（可能包含中文等非 ASCII 字符）转换为纯 ASCII 的临时文件路径。
    /// 如果源文件已复制过，直接返回缓存路径；否则复制到 %TEMP% 并缓存。
    /// </summary>
    private static string GetAsciiSafePath(string originalPath)
    {
        lock (_pathCacheLock)
        {
            if (_cachedAsciiPaths.TryGetValue(originalPath, out var cached))
                return cached;
        }

        bool sourceExists;
        if (originalPath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
            sourceExists = Godot.FileAccess.FileExists(originalPath);
        else
            sourceExists = File.Exists(originalPath);

        if (!sourceExists)
            return originalPath;

        byte[] hashBytes;
        using (var md5 = MD5.Create())
        {
            hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(originalPath));
        }
        var hashName = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        var extension = Path.GetExtension(originalPath);
        if (string.IsNullOrEmpty(extension))
            extension = ".mp3";

        var tempDir = Path.Combine(Path.GetTempPath(), "CuteSakikoModAudio");
        Directory.CreateDirectory(tempDir);
        var safePath = Path.Combine(tempDir, hashName + extension);

        if (!File.Exists(safePath))
        {
            try
            {
                byte[] data;
                if (originalPath.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
                {
                    using var file = Godot.FileAccess.Open(originalPath, Godot.FileAccess.ModeFlags.Read);
                    if (file == null)
                        return originalPath;
                    var length = (long)file.GetLength();
                    data = file.GetBuffer(length);
                }
                else
                {
                    data = File.ReadAllBytes(originalPath);
                }

                File.WriteAllBytes(safePath, data);
            }
            catch
            {
                return originalPath;
            }
        }

        lock (_pathCacheLock)
        {
            _cachedAsciiPaths[originalPath] = safePath;
        }
        return safePath;
    }

    // ---- 音效播放（内部） ----
    private static void PlaySoundInternal(string filePath, float baseVolume)
    {
        var safePath = GetAsciiSafePath(filePath);

        var settings = SaveManager.Instance?.SettingsSave;
        var masterVol = settings?.VolumeMaster ?? 1.0f;
        var sfxVol = settings?.VolumeSfx ?? 1.0f;
        var modSfxVol = ModConfig.ModSfxVolume;
        var finalVol = Mathf.Clamp(baseVolume * masterVol * sfxVol * modSfxVol, 0.0f, 1.0f);

        var handle = FmodStudioStreamingFiles.TryCreateSoundHandle(safePath);
        if (handle == null)
        {
            FmodStudioStreamingFiles.TryPlaySoundFile(safePath, finalVol);
            return;
        }

        handle.RawInstance.Call("set_volume", finalVol);
        handle.RawInstance.Call("play");

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

    // ---- 音乐播放（内部） ----
    private static AudioMusicHandle? PlayMusicInternal(string filePath, float baseVolume)
    {
        var safePath = GetAsciiSafePath(filePath);

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
        var finalVol = Mathf.Clamp(baseVolume * masterVol * modBgmVol * bgmVol, 0.0f, 1.0f);

        var options = new AudioPlaybackOptions { Scope = AudioLifecycleScope.Combat };
        var handle = FmodStudioStreamingFiles.TryCreateStreamingMusicHandle(safePath, options);
        if (handle != null)
        {
            handle.RawInstance.Call("set_volume", finalVol);
            handle.RawInstance.Call("play");
            handle.RawInstance.Call("set_loop_count", -1);
            _currentMusicHandle = handle;
            _currentMusicPath = safePath;
        }
        return handle;
    }

    // ---- 刷新音乐音量（内部） ----
    private static void RefreshMusicVolumeInternal()
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
        var finalVol = Mathf.Clamp(1.0f * masterVol * bgmVol * modBgmVol, 0.0f, 1.0f);
        _currentMusicHandle.RawInstance.Call("set_volume", finalVol);
    }

    // ---- 停止音乐（内部） ----
    private static void StopMusicInternal()
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
}