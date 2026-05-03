using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;

namespace CuteSakikoMod.CuteSakikoModCode.Events;

public class PracticeGuitarOption : RestSiteOption
{
    private readonly Player _player;
    private readonly AnonGuitar _relic;

    public PracticeGuitarOption(Player player, AnonGuitar relic)
        : base(player)
    {
        _player = player;
        _relic = relic;
    }

    public override string OptionId => "PracticeGuitar";

    public override LocString Description => new("rest_site_ui", "PRACTICE_GUITAR_DESC");

    public override async Task<bool> OnSelect()
    {
        if (!IsEnabled) return false;

        var rng = _player.RunState.Rng.UpFront;

        // 替换三个主槽位
        foreach (var cat in Enum.GetValues<ChordCategory>())
        {
            if (cat == ChordCategory.Bonus) continue;
            var pool = ChordManager.GetLearnableChordIds(cat);
            if (pool.Count == 0) continue;
            _relic.ReplaceChord(cat, rng.NextItem(pool));
        }

        // 处理 Bonus 槽位
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

        // 标记练习已完成，且自身变灰
        _relic.MarkPracticeUsed();
        IsEnabled = false;
        return true;
    }
}