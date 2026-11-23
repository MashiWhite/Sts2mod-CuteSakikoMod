using CuteSakikoMod.CuteSakikoModCode.Cards.Mod;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Status;

/// <summary>
///     汉堡：状态卡，保留、无法被打出。由「我要吃汉堡」生成。
/// </summary>
public class Hamburger() : ModStatusCard(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Retain, CardKeyword.Unplayable];

    public override int MaxUpgradeLevel => 0;
}
