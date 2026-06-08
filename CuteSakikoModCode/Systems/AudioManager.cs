using Godot;
using MegaCrit.Sts2.Core.Saves;
using STS2RitsuLib.Audio;
using CuteSakikoMod.CuteSakikoModCode.Others;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class AudioManager
{
    private static readonly Stack<float> _bgmVolumeStack = new();
    private static AudioMusicHandle? _currentMusicHandle;

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
        StopMusic();

        var settings = SaveManager.Instance?.SettingsSave;
        var masterVol = settings?.VolumeMaster ?? 1.0f;
        var modBgmVol = ModConfig.ModBgmVolume;
        var finalVol = Mathf.Clamp(baseVolume * masterVol * modBgmVol, 0.0f, 1.0f);

        if (settings != null)
        {
            _bgmVolumeStack.Push(settings.VolumeBgm);
            settings.VolumeBgm = 0f;
        }

        var options = new AudioPlaybackOptions { Scope = AudioLifecycleScope.Combat };
        var handle = FmodStudioStreamingFiles.TryCreateStreamingMusicHandle(filePath, options);
        if (handle != null)
        {
            handle.RawInstance.Call("set_volume", finalVol);
            handle.RawInstance.Call("play");
            handle.RawInstance.Call("set_loop_count", -1);
            _currentMusicHandle = handle;
        }
        else
        {
            RestoreBgmVolume();
        }

        return handle;
    }

    public static void StopMusic()
    {
        if (_currentMusicHandle != null)
        {
            if (GodotObject.IsInstanceValid(_currentMusicHandle.RawInstance))
                _currentMusicHandle.RawInstance.Call("stop");
            _currentMusicHandle = null;
        }

        RestoreBgmVolume();
    }

    public static void RefreshMusicVolume()
    {
        if (_currentMusicHandle == null || !GodotObject.IsInstanceValid(_currentMusicHandle.RawInstance))
        {
            _currentMusicHandle = null;
            return;
        }

        var settings = SaveManager.Instance?.SettingsSave;
        var masterVol = settings?.VolumeMaster ?? 1.0f;
        var modBgmVol = ModConfig.ModBgmVolume;
        var finalVol = Mathf.Clamp(1.0f * masterVol * modBgmVol, 0.0f, 1.0f);
        _currentMusicHandle.RawInstance.Call("set_volume", finalVol);
    }

    private static void RestoreBgmVolume()
    {
        if (_bgmVolumeStack.Count > 0)
        {
            var settings = SaveManager.Instance?.SettingsSave;
            if (settings != null)
                settings.VolumeBgm = _bgmVolumeStack.Pop();
        }
    }
}