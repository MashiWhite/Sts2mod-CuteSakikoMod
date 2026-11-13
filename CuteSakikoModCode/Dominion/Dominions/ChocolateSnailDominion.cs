using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Dread;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Dominion.Dominions;

/// <summary>
///     巧克力螺的统御（恐惧模式）：本幕全体小怪各获得 4 层再生。
/// </summary>
public class ChocolateSnailDominion : DominionModel
{
    private const int RegenAmount = 4;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromPowerWithPowerHoverTips<RegenPower>(RegenAmount);

    protected override Task AfterEnemiesEnteredCombat(IEnumerable<Creature> creatures)
    {
        return PowerCmd.Apply<RegenPower>(
            new ThrowingPlayerChoiceContext(), creatures, RegenAmount, null, null);
    }
}
