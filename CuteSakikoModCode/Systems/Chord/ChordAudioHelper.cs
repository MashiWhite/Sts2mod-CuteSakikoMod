using System.Reflection;

// AudioManager 所在命名空间，按你项目实际路径调整

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Chord;

/// <summary>
///     和弦系统的音频播放工具，独立于吉他遗物。
/// </summary>
public static class ChordAudioHelper
{
    private static readonly string AudioDir = AudioManager.AudioDirectory;

    private static readonly string[] StrumFiles =
        { "guitar_strum1.mp3", "guitar_strum2.mp3", "guitar_strum3.mp3", "guitar_strum4.mp3", "guitar_strum5.mp3" };

    private static readonly Random _rand = new();

    /// <summary>播放一次随机的吉他扫弦音效。</summary>
    public static void PlayStrumSound()
    {
        var sfx = Path.Combine(AudioDir, StrumFiles[_rand.Next(StrumFiles.Length)]);
        AudioManager.PlaySound(sfx);
    }
}