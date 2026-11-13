using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Dread;

namespace CuteSakikoMod.CuteSakikoModCode.Dominion.Dominions;

/// <summary>
///     立希的统御（恐惧模式）：本幕全体小怪获得弱化版十日余命（10 层，每敌方回合 -1，按层触发基础效果）。
/// </summary>
public class TenDayDominion : DominionModel
{
    private const int Days = 10;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromPowerWithPowerHoverTips<TenDayDreadPower>(Days);

    protected override Task AfterEnemiesEnteredCombat(IEnumerable<Creature> creatures)
    {
        return PowerCmd.Apply<TenDayDreadPower>(
            new ThrowingPlayerChoiceContext(), creatures, Days, null, null);
    }
}
