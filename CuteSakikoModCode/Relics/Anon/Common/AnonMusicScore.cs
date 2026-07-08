using CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;
using CuteSakikoMod.CuteSakikoModCode.Systems;
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

        // 收集当前已拥有的所有和弦 ID
        var ownedChordIds = new HashSet<string>();
        foreach (var kv in guitar.GetCurrentChords())
            if (!string.IsNullOrEmpty(kv.Value))
                ownedChordIds.Add(kv.Value);
        foreach (var id in guitar.GetBonusChords())
            ownedChordIds.Add(id);

        // 构建候选池（所有可学习的大、小、属七和弦）
        var allPools = new List<string>();
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));

        // 排除已拥有的和弦
        var available = allPools.Where(id => !ownedChordIds.Contains(id)).ToList();

        if (available.Count > 0)
        {
            var rng = Owner.RunState.Rng.UpFront;
            guitar.AddBonusChord(rng.NextItem(available));
        }
        // 若 available 为空，则表示所有可学和弦都已拥有，不再添加
    }
}