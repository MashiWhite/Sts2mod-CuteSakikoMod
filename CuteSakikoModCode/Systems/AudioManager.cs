using System.IO;
using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Others.Config;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Saves;
using FileAccess = Godot.FileAccess;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class AudioManager
{
    // ---- 内部节点 ----
    private static Node? _audioRoot;

    private static string? _audioDir;

    /// <summary>
    /// 加载器结构下程序集位于 compat/&lt;游戏版本&gt;/，而音频固定在 mod 根目录的 audio/。
    /// 因此从程序集目录向上逐层查找第一个真实存在的 audio 目录。
    /// </summary>
    public static string AudioDirectory
    {
        get
        {
            if (_audioDir != null) return _audioDir;
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            for (var i = 0; i < 5 && !string.IsNullOrEmpty(dir); i++)
            {
                var candidate = Path.Combine(dir, "audio");
                if (Directory.Exists(candidate))
                {
                    _audioDir = candidate;
                    return candidate;
                }
                dir = Path.GetDirectoryName(dir);
            }
            _audioDir = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty, "audio");
            return _audioDir;
        }
    }

    public static string ResolveAudioPath(string fileName) => Path.Combine(AudioDirectory, fileName);

    // ---- 音乐播放器 ----
    private static AudioStreamPlayer? _musicPlayer;
    private static string? _currentMusicPath;
    private static bool _nativeMusicStopped;

    // ---- 音效去重字典 ----
    private static readonly Dictionary<string, AudioStreamPlayer> _activeSfx = new();

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
    private static bool IsMainThread()
    {
        return OS.GetMainThreadId() == OS.GetThreadCallerId();
    }

    private static void EnsureAudioRoot()
    {
        if (_audioRoot != null && GodotObject.IsInstanceValid(_audioRoot))
            return;
        var sceneTree = Engine.GetMainLoop() as SceneTree;
        var root = sceneTree?.Root;
        if (root == null) return;
        _audioRoot = new Node { Name = "CuteSakikoAudio" };
        root.AddChild(_audioRoot);
    }

    private static AudioStream? LoadAudioStream(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        try
        {
            byte[] data;
            if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
            {
                using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
                if (file == null) return null;
                data = file.GetBuffer((long)file.GetLength());
            }
            else
            {
                if (!File.Exists(path)) return null;
                data = File.ReadAllBytes(path);
            }

            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext switch
            {
                ".mp3" => new AudioStreamMP3 { Data = data },
                ".ogg" => AudioStreamOggVorbis.LoadFromBuffer(data),
                ".wav" => AudioStreamWav.LoadFromBuffer(data),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static void PlaySoundInternal(string filePath, float baseVolume)
    {
        var stream = LoadAudioStream(filePath);
        if (stream == null)
        {
            GD.PushWarning($"[CuteSakikoMod] 音效加载失败: {filePath}");
            return;
        }
        EnsureAudioRoot();
        if (_audioRoot == null) return;

        // 去重：停止同路径旧音效
        if (_activeSfx.TryGetValue(filePath, out var existing))
        {
            existing.Stop();
            existing.QueueFree();
            _activeSfx.Remove(filePath);
        }

        var player = new AudioStreamPlayer();
        _audioRoot.AddChild(player);
        player.Stream = stream;
        player.Bus = "Master";

        var linearVol = CalculateFinalSfxVolume(baseVolume);
        var dbVol = LinearToDb(linearVol);
        player.VolumeDb = dbVol;
        player.Play();

        player.Finished += () =>
        {
            _activeSfx.Remove(filePath);
            player.QueueFree();
        };
        _activeSfx[filePath] = player;
    }

    private static AudioStreamPlayer? PlayMusicInternal(string filePath, float baseVolume)
    {
        if (_currentMusicPath == filePath && _musicPlayer?.Playing == true)
            return _musicPlayer;
        var stream = LoadAudioStream(filePath);
        if (stream == null)
        {
            GD.PushWarning($"[CuteSakikoMod] 音乐加载失败: {filePath}");
            return null;
        }
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
        var linearVol = CalculateFinalBgmVolume(baseVolume);
        var dbVol = LinearToDb(linearVol);
        _musicPlayer.VolumeDb = dbVol;
        _musicPlayer.Finished += () =>
        {
            if (_musicPlayer?.Playing == false) _musicPlayer.Play();
        };
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
            var linearVol = CalculateFinalBgmVolume(1f);
            _musicPlayer.VolumeDb = LinearToDb(linearVol);
        }
    }

    private static float CalculateFinalSfxVolume(float baseVolume)
    {
        var settings = SaveManager.Instance?.SettingsSave;
        var master = settings?.VolumeMaster ?? 1f;
        var sfx = settings?.VolumeSfx ?? 1f;
        var mod = ModConfig.ModSfxVolume;
        return Mathf.Clamp(baseVolume * master * sfx * mod, 0f, 1f);
    }

    private static float CalculateFinalBgmVolume(float baseVolume)
    {
        var settings = SaveManager.Instance?.SettingsSave;
        var master = settings?.VolumeMaster ?? 1f;
        var bgm = settings?.VolumeBgm ?? 1f;
        var mod = ModConfig.ModBgmVolume;
        return Mathf.Clamp(baseVolume * master * bgm * mod, 0f, 1f);
    }

    private static float LinearToDb(float linear)
    {
        if (linear <= 0f) return -80f;
        return 20f * Mathf.Log(linear) / Mathf.Log(10f);
    }
}