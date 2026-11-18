namespace CuteSakikoMod.CuteSakikoModCode.CardPiles;

/// <summary>
///     让能力/卡牌参与「种植槽上限」计算：实现该接口的 PowerModel 每层按其返回值加槽位。
///     目前没有具体实现，留给以后的「增加种植槽」类卡牌/能力。
/// </summary>
public interface IPlantingCapacityBonus
{
    /// <summary>额外种植槽数量（可正可负）。</summary>
    int BonusPlantingSlots { get; }
}
