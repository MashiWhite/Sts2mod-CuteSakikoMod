using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

public class PracticeGuitarOption : ModRestSiteOptionTemplate
{
    private readonly AnonGuitar _relic;

    public PracticeGuitarOption(Player player, AnonGuitar relic) : base(player)
    {
        _relic = relic;
    }

    public override string OptionId => "PracticeGuitar";

    public override LocString Description => new("rest_site_ui", "PRACTICE_GUITAR_DESC");

    public override RestSiteOptionAssetProfile AssetProfile => new(
        IconPath: "res://CuteSakikoMod/images/ui/rest_site/practice_guitar.png"
    );

    public override LocString? CustomTitle => new LocString("rest_site_ui", "OPTION_PRACTICE_GUITAR.name");

    public override async Task<bool> OnSelect()
    {
        if (_relic.PracticeUsedThisVisit) return false;
        if (!IsEnabled) return false;

        var rng = Owner.RunState.Rng.UpFront;
        foreach (var cat in Enum.GetValues<ChordCategory>())
        {
            if (cat == ChordCategory.Bonus) continue;
            var pool = ChordManager.GetLearnableChordIds(cat);
            if (pool.Count == 0) continue;
            _relic.ReplaceChord(cat, rng.NextItem(pool));
        }

        var bonusChords = _relic.GetBonusChords();
        if (bonusChords.Count > 0)
        {
            var allPools = new List<string>();
            allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
            allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
            allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));
            if (allPools.Count > 0)
            {
                var bonusCount = bonusChords.Count;
                var oldIds = bonusChords.ToList();
                foreach (var oldId in oldIds) _relic.RemoveBonusChord(oldId);
                for (var i = 0; i < bonusCount; i++) _relic.AddBonusChord(rng.NextItem(allPools));
            }
        }

        if (LocalContext.IsMe(Owner)) _relic.PracticeUsedThisVisit = true;
        return true;
    }
}