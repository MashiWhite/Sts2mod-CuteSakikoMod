using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Dread;

namespace CuteSakikoMod.CuteSakikoModCode.Dominion.Dominions;

/// <summary>
///     藤都子的统御（恐惧模式）：本幕全体小怪获得弱化版男子禁止（单人 15%/15%，多人每人 5%/5%）。
/// </summary>
public class MiyakoDominion : DominionModel
{
    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromPowerWithPowerHoverTips<WeakenedMiyakoNoBoysPower>(1);

    protected override Task AfterEnemiesEnteredCombat(IEnumerable<Creature> creatures)
    {
        return PowerCmd.Apply<WeakenedMiyakoNoBoysPower>(
            new ThrowingPlayerChoiceContext(), creatures, 1m, null, null);
    }
}
