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

    private bool _bonusAdded;

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        _initialized = false;
        EnsureInitialized();
    }

    public override async Task AfterObtained()
    {
        // 尝试从静态字典恢复（进化瞬间传递）
        if (Owner != null && _pendingMigration.TryGetValue(Owner, out var data))
        {
            RestoreChordData(data.chords, data.bonus, data.temp);
            _pendingMigration.Remove(Owner);
        }
        else if (Owner != null)
        {
            // 静态字典没有，尝试从可能还存在的旧吉他复制
            var oldGuitar = Owner.Relics.OfType<AnonGuitar>()
                .FirstOrDefault(r => r is not FlashAnonGuitar && r != this);
            if (oldGuitar != null)
            {
                oldGuitar.CopyChordsTo(this);
            }
        }

        // 调用基类 AfterObtained，确保 EnsureInitialized 执行
        await base.AfterObtained();

        // 清理旧的 bonus 迁移数据
        _pendingBonusMigration.Remove(Owner);

        // 添加专属 Bonus 和弦
        if (!_bonusAdded)
        {
            _bonusAdded = true;
            if (Owner != null)
            {
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
}