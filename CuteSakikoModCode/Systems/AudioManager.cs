using Godot;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Audio;
using CuteSakikoMod.CuteSakikoModCode.Others;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class AudioManager
{
    private static AudioMusicHandle? _currentMusicHandle;
    private static string? _currentMusicPath;
    private static bool _nativeMusicStopped;

    public static void PlaySound(string filePath, float baseVolume = 1.0f)
    {
        var settings = SaveManager.Instance?.SettingsSave;
        var masterVol = settings?.VolumeMaster ?? 1.0f;
        var sfxVol = settings?.VolumeSfx ?? 1.0f;
        var finalVol = Mathf.Clamp(baseVolume * masterVol * sfxVol, 0.0f, 1.0f);
        FmodStudioStreamingFiles.TryPlaySoundFile(filePath, finalVol);
    }

    public static AudioMusicHandle? PlayMusic(string filePath, float baseVolume = 1.0f)
    {
        // 如果已经播放同一首音乐，则直接返回当前句柄，不做任何操作
        if (_currentMusicHandle != null &&
            GodotObject.IsInstanceValid(_currentMusicHandle.RawInstance) &&
            _currentMusicPath == filePath)
        {
            return _currentMusicHandle;
        }

        // 停止之前的 Mod 音乐
        StopMusicInternal();

        // 第一次播放 Mod 音乐时，停止原生 BGM
        if (!_nativeMusicStopped)
        {
            NRunMusicController.Instance?.StopMusic();
            _nativeMusicStopped = true;
        }

        var settings = SaveManager.Instance?.SettingsSave;
        var masterVol = settings?.VolumeMaster ?? 1.0f;
        var modBgmVol = ModConfig.ModBgmVolume;
        var finalVol = Mathf.Clamp(baseVolume * masterVol * modBgmVol, 0.0f, 1.0f);

        var options = new AudioPlaybackOptions { Scope = AudioLifecycleScope.Combat };
        var handle = FmodStudioStreamingFiles.TryCreateStreamingMusicHandle(filePath, options);
        if (handle != null)
        {
            handle.RawInstance.Call("set_volume", finalVol);
            handle.RawInstance.Call("play");
            handle.RawInstance.Call("set_loop_count", -1);
            _currentMusicHandle = handle;
            _currentMusicPath = filePath;
        }
        return handle;
    }

    public static void StopMusic()
    {
        StopMusicInternal();
        RestoreNativeMusicIfNeeded();
    }

    public static void RefreshMusicVolume()
    {
        if (_currentMusicHandle == null || !GodotObject.IsInstanceValid(_currentMusicHandle.RawInstance))
        {
            _currentMusicHandle = null;
            _currentMusicPath = null;
            return;
        }

        var settings = SaveManager.Instance?.SettingsSave;
        var masterVol = settings?.VolumeMaster ?? 1.0f;
        var modBgmVol = ModConfig.ModBgmVolume;
        var finalVol = Mathf.Clamp(1.0f * masterVol * modBgmVol, 0.0f, 1.0f);
        _currentMusicHandle.RawInstance.Call("set_volume", finalVol);
    }

    private static void StopMusicInternal()
    {
        if (_currentMusicHandle != null)
        {
            if (GodotObject.IsInstanceValid(_currentMusicHandle.RawInstance))
                _currentMusicHandle.RawInstance.Call("stop");
            _currentMusicHandle = null;
            _currentMusicPath = null;
        }
    }

    private static void RestoreNativeMusicIfNeeded()
    {
        if (!_nativeMusicStopped) return;
        // 让游戏自身恢复原生 BGM（会根据当前房间自动选择合适的音乐）
        NRunMusicController.Instance?.UpdateMusic();
        _nativeMusicStopped = false;
    }
}