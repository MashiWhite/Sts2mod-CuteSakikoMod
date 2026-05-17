
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Rare;
using CuteSakikoMod.CuteSakikoModCode.Cards.Anon.Token;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public class LittleMomentsManager : HookedSingletonModel
{
    public LittleMomentsManager() : base(true, false) { }

    public override bool ShouldReceiveCombatHooks => true;

    // 玩家回合开始时：将消耗堆中的所有 LittleMoments 和 Lifetime 移到弃牌堆
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        var exhaustPile = PileType.Exhaust.GetPile(player);
        if (exhaustPile == null) return;

        // 收集所有的 LittleMoments 和 Lifetime
        var cardsToMove = exhaustPile.Cards
            .Where(c => c is LittleMoments || c is Lifetime)
            .ToList();

        if (cardsToMove.Count > 0)
            await CardPileCmd.Add(cardsToMove, PileType.Discard);
    }

    // 玩家回合结束时：检查弃牌堆、消耗堆和抽牌堆，合成“一辈子”
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != CombatSide.Player)
            return;

        var players = RunManager.Instance.DebugOnlyGetState()?.Players;
        if (players == null) return;

        foreach (var player in players)
        {
            if (player.Creature?.CombatState == null) continue;

            // 检查弃牌堆、抽牌堆和消耗堆
            await TryMergeInPile(player, PileType.Discard);
            await TryMergeInPile(player, PileType.Draw);
            await TryMergeInPile(player, PileType.Exhaust);
        }
    }

    private async Task TryMergeInPile(Player player, PileType pileType)
    {
        var pile = pileType.GetPile(player);
        if (pile == null) return;

        var littleMoments = pile.Cards.OfType<LittleMoments>().ToList();
        if (littleMoments.Count < 5) return;

        bool anyUpgraded = littleMoments.Any(c => c.IsUpgraded);

        await CardPileCmd.RemoveFromCombat(littleMoments);

        var combatState = player.Creature.CombatState;
        var lifetime = combatState.CreateCard<Lifetime>(player);
        if (anyUpgraded)
        {
            lifetime.UpgradeInternal();
            lifetime.FinalizeUpgradeInternal();
        }

        await CardPileCmd.Add(lifetime, pile, CardPilePosition.Top);
    }
}