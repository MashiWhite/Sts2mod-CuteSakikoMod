using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Planting;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Mutsumi.Starter;

/// <summary>
///     睦的初始遗物「黄瓜种子」：每场战斗第一回合开始时，将 1 张黄瓜种入种植牌堆。
///     种植堆满 3 张时不再追加（与种小黄瓜的容量规则一致）。
/// </summary>
public class CucumberSeed : CuteMutsumiRelic
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task AfterSideTurnStart(
        CombatSide side,
        IReadOnlyList<Creature> participants,
        ICombatState combatState)
    {
        if (Owner == null) return;
        if (side != Owner.Creature.Side || combatState.RoundNumber != 1) return;
        if (!PlantingCardPile.CanPlant(Owner, 1)) return;

        var cucumber = combatState.CreateCard<Cucumber>(Owner);
        if (cucumber is PlantingCardBase plant)
            plant.PlantedTurn = Owner.PlayerCombatState?.TurnNumber ?? 0;

        await CardPileCmd.AddGeneratedCardToCombat(cucumber, PlantingCardPile.GetPileType(), Owner);
        Flash();
    }
}
