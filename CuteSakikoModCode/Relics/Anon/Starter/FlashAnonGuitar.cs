using System.Runtime.Serialization;
using CuteSakikoMod.CuteSakikoModCode.Systems;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Anon.Starter;

public class FlashAnonGuitar : AnonGuitar
{
    public override RelicRarity Rarity => RelicRarity.Starter;
    protected override int MaxLearnedChordsPerCategory => 2;
    protected override int FirstPlayBonus => 3; // 新增这一行

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        _initialized = false;
        EnsureInitialized();
    }

    public override async Task AfterObtained()
    {
        if (Owner != null && _pendingMigration.TryGetValue(Owner, out var data))
        {
            RestoreChordData(data.chords, data.bonus, data.temp);
            _pendingMigration.Remove(Owner);
        }
        else if (Owner != null)
        {
            var oldGuitar = Owner.Relics.OfType<AnonGuitar>()
                .FirstOrDefault(r => r is not FlashAnonGuitar && r != this);
            if (oldGuitar != null) oldGuitar.CopyChordsTo(this);
        }

        await base.AfterObtained();
        _pendingBonusMigration.Remove(Owner);

        // 先古吉他：填充所有类别至上限（2个）
        foreach (var cat in new[] { ChordCategory.Major, ChordCategory.Minor, ChordCategory.Dominant })
            FillCategorySlots(cat);
    }
}