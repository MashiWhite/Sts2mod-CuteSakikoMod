using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Random;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class CharacterBanterHelper
{
    private const string LocTable = "characters";
    private const string BanterPrefix = "CUTESAKIKOMOD-CUTE_ANON.banter.";

    /// <summary>
    ///     尝试让爱音说出台词（联机同步版本）
    /// </summary>
    /// <param name="speaker">说话者</param>
    /// <param name="actionType">动作类型：attack / skill / power</param>
    /// <param name="rng">同步随机数生成器（必须来自 RunState.Rng.UpFront）</param>
    public static void TrySayBanter(Creature speaker, string actionType, Rng rng)
    {
        // 只处理爱音
        if (speaker?.Player?.Character?.Id.Entry != "CUTESAKIKOMOD-CUTE_ANON")
            return;

        var keyPrefix = $"{BanterPrefix}{actionType}.";
        var randomLine = LocString.GetRandomWithPrefix(LocTable, keyPrefix, rng);

        if (randomLine != null && !randomLine.IsEmpty) ThinkCmd.Play(randomLine, speaker);
    }
}