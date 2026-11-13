using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models.Dread;

namespace CuteSakikoMod.CuteSakikoModCode.Dominion.Dominions;

/// <summary>
///     灰爱音的统御（恐惧模式）：本幕全体小怪获得弱化版心之壁。
///     同一张牌每轮最多造成 3 次伤害后才免疫（本体是 1 次）。
/// </summary>
public class GreyAnonDominion : DominionModel
{
    private const int Plays = 3;

    public override IEnumerable<IHoverTip> ExtraHoverTips =>
        HoverTipFactory.FromPowerWithPowerHoverTips<HeartWallPower>(Plays);

    protected override Task AfterEnemiesEnteredCombat(IEnumerable<Creature> creatures)
    {
        return PowerCmd.Apply<HeartWallPower>(
            new ThrowingPlayerChoiceContext(), creatures, Plays, null, null);
    }
}
