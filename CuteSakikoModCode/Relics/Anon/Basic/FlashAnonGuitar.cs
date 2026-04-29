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
        // 从静态缓存中获取旧吉他的 Bonus 列表（如果有）
        List<string> oldBonus = null;
        if (_pendingBonusTransfer.TryGetValue(Owner, out var list))
        {
            oldBonus = list;
            _pendingBonusTransfer.Remove(Owner); // 取出后清除
        }

        await base.AfterObtained();

        // 合并旧 Bonus
        if (oldBonus != null)
            foreach (var chord in oldBonus)
                AddBonusChord(chord);

        // 添加闪亮吉他自带的 Bonus
        var rng = Owner.RunState.Rng.UpFront;
        var allPools = new List<string>();
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
        allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));

        if (allPools.Count > 0)
            AddBonusChord(rng.NextItem(allPools));
    }
}