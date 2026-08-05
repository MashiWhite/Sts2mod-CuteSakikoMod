using Godot;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Saves;
using CuteSakikoMod.CuteSakikoModCode.Others;
using System.Collections.Generic;
using STS2RitsuLib;
using FileAccess = Godot.FileAccess;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

/// <summary>
/// 模组音频管理（Godot 原生版）—— 彻底消除 FMOD 崩溃风险。
/// </summary>
public static class AudioManager
{
    // ---- 内部节点 ----
    private static Node? _audioRoot;

    // ---- 音乐播放器 ----
    private static AudioStreamPlayer? _musicPlayer;
    private static string? _currentMusicPath;
    private static bool _nativeMusicStopped;

    // ---- 音效播放器池 ----
    private static readonly List<AudioStreamPlayer> _sfxPlayers = new();
    private const int MaxSfxPlayers = 32;

    // ==================== 公开接口 ====================

    public static void PlaySound(string filePath, float baseVolume = 1f)
    {
        if (!ModConfig.EnableAudio) return;

        if (IsMainThread())
            PlaySoundInternal(filePath, baseVolume);
        else
            Callable.From(() => PlaySoundInternal(filePath, baseVolume)).CallDeferred();
    }

    public static AudioStreamPlayer? PlayMusic(string filePath, float baseVolume = 1f)
    {
        if (!ModConfig.EnableAudio) return null;

        if (IsMainThread())
            return PlayMusicInternal(filePath, baseVolume);

        Callable.From(() => PlayMusicInternal(filePath, baseVolume)).CallDeferred();
        return null;
    }

    public static void RefreshMusicVolume()
    {
        if (IsMainThread()) RefreshMusicVolumeInternal();
        else Callable.From(RefreshMusicVolumeInternal).CallDeferred();
    }

    public static void StopMusic()
    {
        if (IsMainThread()) StopMusicInternal();
        else Callable.From(StopMusicInternal).CallDeferred();
    }

    // ==================== 内部实现 ====================

    private static bool IsMainThread() => OS.GetMainThreadId() == OS.GetThreadCallerId();

    private static void EnsureAudioRoot()
    {
        if (_audioRoot != null && GodotObject.IsInstanceValid(_audioRoot))
            return;

        var sceneTree = Engine.GetMainLoop() as SceneTree;
        var root = sceneTree?.Root;
        if (root == null)
        {
            RitsuLibFramework.Logger.Warn("[AudioManager] SceneTree.Root is null, audio will not play.");
            return;
        }

        _audioRoot = new Node { Name = "CuteSakikoAudio" };
        root.AddChild(_audioRoot);
        RitsuLibFramework.Logger.Info("[AudioManager] Audio root node created on SceneTree root.");
    }

    // ---- 加载音频文件（支持 res:// 和绝对路径） ----
    private static AudioStream? LoadAudioStream(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        try
        {
            byte[] data;
            if (path.StartsWith("res://", System.StringComparison.OrdinalIgnoreCase))
            {
                using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    RitsuLibFramework.Logger.Warn($"[AudioManager] Failed to open res:// file: {path}");
                    return null;
                }
                data = file.GetBuffer((long)file.GetLength());
            }
            else
            {
                if (!File.Exists(path))
                {
                    RitsuLibFramework.Logger.Warn($"[AudioManager] File not found: {path}");
                    return null;
                }
                data = File.ReadAllBytes(path);
            }

            var ext = System.IO.Path.GetExtension(path).ToLowerInvariant();
            switch (ext)
            {
                case ".mp3":
                    return new AudioStreamMP3 { Data = data };
                case ".ogg":
                    return AudioStreamOggVorbis.LoadFromBuffer(data);
                case ".wav":
                    return AudioStreamWav.LoadFromBuffer(data);
                default:
                    return null;
            }
        }
        catch (Exception e)
        {
            RitsuLibFramework.Logger.Error($"[AudioManager] LoadAudioStream failed for {path}: {e.Message}");
            return null;
        }
    }

    // ---- 音效播放 ----
    private static void PlaySoundInternal(string filePath, float baseVolume)
    {
        var stream = LoadAudioStream(filePath);
        if (stream == null) return;

        EnsureAudioRoot();
        if (_audioRoot == null) return;

        var player = GetFreeSfxPlayer();
        player.Stream = stream;
        player.Bus = "Master";

        float linearVol = CalculateFinalSfxVolume(baseVolume);
        float dbVol = LinearToDb(linearVol);
        RitsuLibFramework.Logger.Info($"[AudioManager] Playing {filePath} at linear {linearVol:F2}, dB {dbVol:F1}");
        player.VolumeDb = dbVol;
        player.Play();
    }

    private static AudioStreamPlayer GetFreeSfxPlayer()
    {
        // 查找空闲播放器
        foreach (var p in _sfxPlayers)
        {
            if (!p.Playing) return p;
        }

        // 新建播放器
        var player = new AudioStreamPlayer();
        _audioRoot!.AddChild(player);
        _sfxPlayers.Add(player);

        // 超出上限时移除最旧的（不停止，让它自然结束）
        if (_sfxPlayers.Count > MaxSfxPlayers)
        {
            var oldest = _sfxPlayers[0];
            _sfxPlayers.RemoveAt(0);
            oldest.Finished += () => oldest.QueueFree();
        }
        return player;
    }

    // ---- 音乐播放 ----
    private static AudioStreamPlayer? PlayMusicInternal(string filePath, float baseVolume)
    {
        if (_currentMusicPath == filePath && _musicPlayer?.Playing == true)
            return _musicPlayer;

        var stream = LoadAudioStream(filePath);
        if (stream == null) return null;

        EnsureAudioRoot();
        if (_audioRoot == null) return null;

        StopMusicInternal();

        if (!_nativeMusicStopped)
        {
            NRunMusicController.Instance?.StopMusic();
            _nativeMusicStopped = true;
        }

        _musicPlayer = new AudioStreamPlayer();
        _audioRoot.AddChild(_musicPlayer);
        _musicPlayer.Stream = stream;
        _musicPlayer.Bus = "Master";

        float linearVol = CalculateFinalBgmVolume(baseVolume);
        float dbVol = LinearToDb(linearVol);
        _musicPlayer.VolumeDb = dbVol;
        _musicPlayer.Finished += () => { if (_musicPlayer?.Playing == false) _musicPlayer.Play(); };
        _musicPlayer.Play();
        _currentMusicPath = filePath;
        return _musicPlayer;
    }

    private static void StopMusicInternal()
    {
        _musicPlayer?.Stop();
        _musicPlayer?.QueueFree();
        _musicPlayer = null;
        _currentMusicPath = null;

        if (_nativeMusicStopped)
        {
            NRunMusicController.Instance?.UpdateMusic();
            _nativeMusicStopped = false;
        }
    }

    private static void RefreshMusicVolumeInternal()
    {
        if (_musicPlayer != null && GodotObject.IsInstanceValid(_musicPlayer))
        {
            float linearVol = CalculateFinalBgmVolume(1f);
            _musicPlayer.VolumeDb = LinearToDb(linearVol);
        }
    }

    // ---- 音量计算 ----
    private static float CalculateFinalSfxVolume(float baseVolume)
    {
        var settings = SaveManager.Instance?.SettingsSave;
        float master = settings?.VolumeMaster ?? 1f;
        float sfx = settings?.VolumeSfx ?? 1f;
        float mod = ModConfig.ModSfxVolume;
        return Mathf.Clamp(baseVolume * master * sfx * mod, 0f, 1f);
    }

    private static float CalculateFinalBgmVolume(float baseVolume)
    {
        var settings = SaveManager.Instance?.SettingsSave;
        float master = settings?.VolumeMaster ?? 1f;
        float bgm = settings?.VolumeBgm ?? 1f;
        float mod = ModConfig.ModBgmVolume;
        return Mathf.Clamp(baseVolume * master * bgm * mod, 0f, 1f);
    }

    private static float LinearToDb(float linear)
    {
        if (linear <= 0f) return -80f;
        return 20f * Mathf.Log(linear) / Mathf.Log(10f);
    }
}