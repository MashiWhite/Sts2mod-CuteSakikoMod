using CuteSakikoMod.CuteSakikoModCode.Cards.Mod;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Status;

/// <summary>
///     筋疲力尽：状态卡，无法被打出，虚无。抽到该牌时，将能量降低至 1 点。
/// </summary>
public class Exhausted() : ModStatusCard(-1, CardType.Status, CardRarity.Status, TargetType.None)
{
    public override IEnumerable<CardKeyword> CanonicalKeywords =>
        [CardKeyword.Unplayable, CardKeyword.Ethereal];

    public override int MaxUpgradeLevel => 0;

    public override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw)
    {
        if (card != this || Owner == null) return;
        if (Owner.PlayerCombatState is not { } state) return;
        if (state.Energy <= 1) return;

        await PlayerCmd.LoseEnergy(state.Energy - 1, Owner);
    }
}
