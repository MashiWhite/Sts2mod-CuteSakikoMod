using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Dread;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CuteSakikoMod.CuteSakikoModCode.Dominion.Dominions;

/// <summary>
///     星爱音的统御（恐惧模式）：敌方回合结束时，全局飞返打出次数 +1，
///     并让本幕全体小怪获得「1 + 飞返打出次数%」点力量。
///     小怪身上挂 <see cref="StarAnonDominionPower" /> 展示实时数值。
/// </summary>
public class StarAnonDominion : DominionModel
{
    private const int FlybackBonusPerTurn = 1;

    protected override Task AfterEnemiesEnteredCombat(IEnumerable<Creature> creatures)
    {
        return PowerCmd.Apply<StarAnonDominionPower>(
            new ThrowingPlayerChoiceContext(), creatures, 1, null, null);
    }

    public override async Task AfterSideTurnEnd(
        PlayerChoiceContext choiceContext,
        CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != CombatSide.Enemy) return;

        var enemies = participants.Where(c => c.Side == CombatSide.Enemy && c.IsAlive).ToList();
        if (enemies.Count == 0) return;

        var manager = FlybackManager.Instance;
        manager?.AddGlobalBonusPlayCount(FlybackBonusPerTurn);

        var count = manager?.TotalPlayCount ?? 0;
        var strength = 1m + count / 100m;
        await PowerCmd.Apply<StrengthPower>(
            new ThrowingPlayerChoiceContext(), enemies, strength, null, null);
    }
}
