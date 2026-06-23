using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Context;
using STS2RitsuLib.Scaffolding.Content;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

public class PracticeGuitarOption : ModRestSiteOptionTemplate
{
    private readonly AnonGuitar _relic;

    private static readonly LocString _titleLoc = new(
        "rest_site_ui", "CUTE_SAKIKO_MOD_OPTION_PRACTICE_GUITAR_NAME");
    private static readonly LocString _descLoc = new(
        "rest_site_ui", "CUTE_SAKIKO_MOD_PRACTICE_GUITAR_DESC");
    private static readonly LocString _promptLoc = new(
        "rest_site_ui", "CUTE_SAKIKO_MOD_PRACTICE_GUITAR_SELECT_PROMPT");

    public PracticeGuitarOption(Player player, AnonGuitar relic) : base(player)
    {
        _relic = relic;
    }

    public override string OptionId => "PracticeGuitar";

    public override LocString Description => _descLoc;

    public override RestSiteOptionAssetProfile AssetProfile => new(
        IconPath: "res://CuteSakikoMod/images/ui/rest_site/practice_guitar.png"
    );

    public override LocString? CustomTitle => _titleLoc;

    public override async Task<bool> OnSelect()
    {
        if (_relic.PracticeUsedThisVisit) return false;
        if (!IsEnabled) return false;

        var allCurrent = new List<string>();
        foreach (ChordCategory cat in Enum.GetValues<ChordCategory>())
        {
            if (cat == ChordCategory.Bonus || cat == ChordCategory.Anon) continue;
            if (_relic.GetCurrentChords().TryGetValue(cat, out var chordId) && !string.IsNullOrEmpty(chordId))
                allCurrent.Add(chordId);
        }
        allCurrent.AddRange(_relic.GetBonusChords());

        if (allCurrent.Count == 0)
            return false;

        var retainedIds = await ChordSelectCmd.SelectChordsForPractice(
            Owner,
            allCurrent,
            _promptLoc
        );

        if (retainedIds.Count == 0)
            return false;

        var rng = Owner.RunState.Rng.UpFront;
        var keepSet = new HashSet<string>(retainedIds);

        foreach (ChordCategory cat in Enum.GetValues<ChordCategory>())
        {
            if (cat == ChordCategory.Bonus || cat == ChordCategory.Anon) continue;
            if (!_relic.GetCurrentChords().TryGetValue(cat, out var currentId) || string.IsNullOrEmpty(currentId))
                continue;

            if (!keepSet.Contains(currentId))
            {
                var pool = ChordManager.GetLearnableChordIds(cat);
                if (pool.Count > 0)
                    _relic.ReplaceChord(cat, rng.NextItem(pool));
            }
        }

        var originalBonus = _relic.GetBonusChords().ToList();
        if (originalBonus.Count > 0)
        {
            var keptBonus = originalBonus.Where(id => keepSet.Contains(id)).ToList();
            int removedCount = originalBonus.Count - keptBonus.Count;

            foreach (var id in originalBonus)
                _relic.RemoveBonusChord(id);
            foreach (var id in keptBonus)
                _relic.AddBonusChord(id);

            if (removedCount > 0)
            {
                var allPools = new List<string>();
                allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
                allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
                allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));
                if (allPools.Count > 0)
                {
                    for (int i = 0; i < removedCount; i++)
                        _relic.AddBonusChord(rng.NextItem(allPools));
                }
            }
        }

        if (LocalContext.IsMe(Owner))
            _relic.PracticeUsedThisVisit = true;

        return true;
    }
}