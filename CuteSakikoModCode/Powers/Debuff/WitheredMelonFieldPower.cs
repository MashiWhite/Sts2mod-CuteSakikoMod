using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Powers;
using MegaCrit.Sts2.Core.Entities.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;

/// <summary>
///     枯萎的瓜田：每层使种植槽上限 -1。
///     由「枯萎的瓜田」能力牌施加；种植槽容量由 PlantingCardPile.GetCapacity 聚合。
/// </summary>
public sealed class WitheredMelonFieldPower : CuteSakikoModPower, IPlantingCapacityBonus
{
    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public int BonusPlantingSlots => -Amount;
}
