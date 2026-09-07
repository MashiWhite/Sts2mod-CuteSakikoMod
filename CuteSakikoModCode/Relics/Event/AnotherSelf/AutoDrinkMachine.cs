using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Factories;
using STS2RitsuLib.Interactions.RightClick;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event.AnotherSelf;

public class AutoDrinkMachine : CuteSakikoEventRelic, IModRightClickableRelic
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    private const int GoldCost = 15;

    public bool CanHandleRightClickLocal(ModRightClickContext context)
    {
        var player = context.Player;
        return player is { Gold: >= GoldCost, HasOpenPotionSlots: true }; // 药水栏必须有空位
    }

    public async Task OnRightClick(ModRightClickExecutionContext context)
    {
        var player = context.Player;
        if (player.Gold < GoldCost || !player.HasOpenPotionSlots)
            return;

        var spent = GoldLossType.Spent;
        await PlayerCmd.LoseGold(GoldCost, player, spent);

        var randomPotion = PotionFactory.CreateRandomPotionInCombat(
            player,
            player.RunState.Rng.CombatPotionGeneration
        ).ToMutable();
        await PotionCmd.TryToProcure(randomPotion, player);
    }
}