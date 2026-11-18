using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Planting;

/// <summary>
///     「种植」关键词的悬浮提示。种植槽上限是动态的（默认 3 + 能力/卡牌加成），
///     所以不用 RitsuLib 的静态关键词提示，改为在这里用 {Slots} 变量现算。
/// </summary>
public static class PlantingKeywordTip
{
    public const string Table = "card_keywords";
    public const string TitleKey = "CUTE_SAKIKO_MOD_KEYWORD_PLANTING.title";
    public const string DescriptionKey = "CUTE_SAKIKO_MOD_KEYWORD_PLANTING.full";

    public static IHoverTip Create(Player? player)
    {
        var title = new LocString(Table, TitleKey);
        var description = new LocString(Table, DescriptionKey);
        description.Add("Slots", CardPiles.PlantingCardPile.GetCapacity(player));
        return new HoverTip(title, description);
    }
}
