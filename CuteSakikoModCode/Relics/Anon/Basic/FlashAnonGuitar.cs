using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;

public class FlashAnonGuitar : AnonGuitar
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override int MaxLearnedChordsPerCategory => 2;
    protected override int EffectMultiplier => 2;

    public override async Task AfterObtained()
    {
        // ✅ 从旧吉他继承完整和弦数据
        if (_pendingChordTransfer.TryGetValue(Owner, out var chordData))
        {
            RestoreChordData(chordData.chords, chordData.bonus, chordData.temp);
            _pendingChordTransfer.Remove(Owner);
        }

        // Bonus 数据已通过 RestoreChordData 恢复，此处不再需要从 _pendingBonusTransfer 合并
        _pendingBonusTransfer.Remove(Owner);

        // 继续执行基类的初始化（此时 _initialized 已为 true，不会再覆盖数据）
        await base.AfterObtained();

        // 添加闪亮吉他特有的随机 Bonus
        var rng = Owner.RunState.Rng.UpFront;
        var allPools = new List<string>();
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));

        if (allPools.Count > 0)
            AddBonusChord(rng.NextItem(allPools));
    }
}