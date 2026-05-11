using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Relics;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Basic;

public class FlashAnonGuitar : AnonGuitar
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override int MaxLearnedChordsPerCategory => 2;
    protected override int EffectMultiplier => 2;

    // 记录是否已添加过专属随机Bonus（避免重复添加）
    private bool _bonusAdded;

    /// <summary>反序列化后重新解析持久化数据到运行时集合</summary>
    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        _initialized = false;
        EnsureInitialized();
    }

    public override async Task AfterObtained()
    {
        // 同一会话内从旧吉他迁移数据（如果存在）
        if (_pendingChordTransfer.TryGetValue(Owner, out var chordData))
        {
            RestoreChordData(chordData.chords, chordData.bonus, chordData.temp);
            _pendingChordTransfer.Remove(Owner);
        }
        _pendingBonusTransfer.Remove(Owner);

        // 基类初始化，从持久化字段恢复已学习的和弦与Bonus
        await base.AfterObtained();

        // 仅初次获得时添加闪亮吉他专有的一个随机Bonus和弦槽位
        if (!_bonusAdded)
        {
            _bonusAdded = true;
            var rng = Owner.RunState.Rng.UpFront;
            var allPools = new List<string>();
            allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Major));
            allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Minor));
            allPools.AddRange(ChordManager.GetLearnableChordIds(ChordCategory.Dominant));

            if (allPools.Count > 0)
                AddBonusChord(rng.NextItem(allPools));
        }
    }
}