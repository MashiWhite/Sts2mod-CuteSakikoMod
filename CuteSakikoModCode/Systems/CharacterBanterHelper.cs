using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Random;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class CharacterBanterHelper
{
    private const string LocTable = "characters";
    private const string BanterPrefix = "CUTE_SAKIKO_MOD_CHARACTER_CUTE_ANON.banter.";

    public static void TrySayBanter(Creature speaker, string actionType, Rng rng)
    {
        if (speaker?.Player?.Character?.Id.Entry != "CUTE_SAKIKO_MOD_CHARACTER_CUTE_ANON")
            return;

        var keyPrefix = $"{BanterPrefix}{actionType}.";
        var randomLine = LocString.GetRandomWithPrefix(LocTable, keyPrefix, rng);

        if (randomLine != null && !randomLine.IsEmpty)
            TalkCmd.Play(randomLine, speaker, VfxColor.White, VfxDuration.Short);
    }
}