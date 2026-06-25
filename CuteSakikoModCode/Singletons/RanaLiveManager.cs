
using CuteSakikoMod.CuteSakikoModCode.Cards.Rana.Rare;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Powers.Buff;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public sealed class RanaLiveManager : HookedSingletonModel
{
    public RanaLiveManager() : base(HookType.Combat) { }

    // 记录本回合已触发过回收的玩家
    private readonly HashSet<ulong> _liveCravingMovedThisTurn = new();

    // 原有：打出 RanaLive 卡牌时给予 1 层莱芜（如果没有莱芜爽）
    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        var card = cardPlay.Card;
        var ownerCreature = card.Owner?.Creature;
        if (ownerCreature == null) return;

        if (!card.Keywords.Contains(CutesakiKeywords.RanaLive.GetModCardKeyword()))
            return;

        if (ownerCreature.HasPower<LiveSweetPower>())
            return;

        await PowerCmd.Apply<RanaLivePower>(choiceContext, ownerCreature, 1, ownerCreature, card);
    }

    // 新增：当 RanaLivePower 层数增加时，尝试回收 LiveCraving
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        if (power is not RanaLivePower || amount <= 0) return;

        var player = power.Owner?.Player;
        if (player == null) return;

        // 本回合已经触发过，跳过
        if (_liveCravingMovedThisTurn.Contains(player.NetId)) return;

        // 从抽牌堆、弃牌堆、消耗堆中寻找 LiveCraving
        var piles = new[] { PileType.Draw, PileType.Discard, PileType.Exhaust };
        CardModel? toMove = null;
        foreach (var pileType in piles)
        {
            var pile = pileType.GetPile(player);
            if (pile != null)
            {
                toMove = pile.Cards.FirstOrDefault(c => c is LiveCraving);
                if (toMove != null) break;
            }
        }

        if (toMove != null)
        {
            await CardPileCmd.Add(toMove, PileType.Hand);
            _liveCravingMovedThisTurn.Add(player.NetId);
        }
    }

    // 回合开始时重置标记
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        _liveCravingMovedThisTurn.Remove(player.NetId);
        await Task.CompletedTask;
    }
}