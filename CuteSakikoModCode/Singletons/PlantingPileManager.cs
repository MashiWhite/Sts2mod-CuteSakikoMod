using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Planting;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

/// <summary>
///     种植牌堆管理器：每个玩家回合开始时，把种植堆里所有卡推进一个阶段。
///     种下当回合不成长，下一回合开始才成长（用卡上的 PlantedTurn 判断）。
/// </summary>
[RegisterSingleton]
public sealed class PlantingPileManager : HookedSingletonModel
{
    public PlantingPileManager() : base(HookType.Combat)
    {
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        var pile = PlantingCardPile.Get(player);
        if (pile == null) return;

        var turn = player.PlayerCombatState?.TurnNumber ?? 0;

        foreach (var plant in pile.Cards.OfType<PlantingCardBase>().ToList())
        {
            // 第一次见到（例如别处生成的种植卡）先记录种植回合，不立刻成长
            if (plant.PlantedTurn < 0)
            {
                plant.PlantedTurn = turn;
                continue;
            }

            if (plant.PlantedTurn >= turn) continue;

            plant.AdvanceStage();
        }

        await Task.CompletedTask;
    }
}
