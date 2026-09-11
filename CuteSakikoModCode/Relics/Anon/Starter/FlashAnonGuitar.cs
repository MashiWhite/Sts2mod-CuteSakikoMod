using System.Runtime.Serialization;
using CuteSakikoMod.CuteSakikoModCode.Systems.Chord;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;

public class FlashAnonGuitar : AnonGuitar
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override int MaxLearnedChordsPerCategory => 2;
    public override int FirstPlayBonus => 3;

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        // 强制下次 EnsureInitialized 重新解析
        _lastSyncedRaw = "";
    }

    public override async Task AfterObtained()
    {
        if (Owner != null && _pendingMigrationTable.TryGetValue(Owner, out var data))
        {
            RestoreChordData(data.Chords, data.Bonus, data.Temp);
            _pendingMigrationTable.Remove(Owner);
        }
        else if (Owner != null)
        {
            var oldGuitar = Owner.Relics.OfType<AnonGuitar>()
                .FirstOrDefault(r => r is not FlashAnonGuitar && r != this);
            if (oldGuitar != null) oldGuitar.CopyChordsTo(this);
        }

        await base.AfterObtained();

        // 先古吉他：填充所有类别至上限（2个）
        foreach (var cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
            FillCategorySlots(cat);
    }
}