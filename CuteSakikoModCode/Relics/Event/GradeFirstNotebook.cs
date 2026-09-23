
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.RestSite;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event;

public class GradeFirstNotebook : CuteSakikoEventRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    public override bool TryModifyRestSiteOptions(Player player, ICollection<RestSiteOption> options)
    {
        if (player != Owner) return false;

        var smith = options.OfType<SmithRestSiteOption>().FirstOrDefault();
        if (smith == null) return false;

        smith.SmithCount += 1;
        return true;
    }
}