using System.Linq;
using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Common;

public class AnonMusicScore : CuteAnonRelic
{
    public override RelicRarity Rarity => RelicRarity.Common;

    public override async Task AfterObtained()
    {
        await base.AfterObtained();

        var guitar = Owner.Relics.OfType<AnonGuitar>().FirstOrDefault();
        if (guitar == null) return;

        ChordCmd.AddRandomBonusChord(guitar);
    }
}