using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class MemoryComingPower : CuteSakikoModPower
{
    private List<CardModel>? _allMemoryCards; // 缓存所有回忆卡牌
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    // 钩子，在玩家回合开始时
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;

        // 延迟初始化：第一次使用时获取所有回忆卡牌
        if (_allMemoryCards == null)
            _allMemoryCards = ModelDb.AllCards
                .Where(card => card.HasModKeyword(CutesakiKeywords.Memory))
                .ToList();

        var exhaustedMemoryIds = SakiMemoryManager.Instance.ExhaustedMemoryIds.ToHashSet(); // 修改这一行

        var availableMemoryCards = _allMemoryCards
            .Where(card => !exhaustedMemoryIds.Contains(card.Id))
            .ToList();

        if (availableMemoryCards.Count == 0) return;

        var count = Amount;
        var randomCards = CardFactory.GetDistinctForCombat(
            Owner.Player,
            availableMemoryCards,
            count,
            Owner.Player.RunState.Rng.CombatCardGeneration
        ).ToList();

        if (randomCards.Count == 0) return;

        var mutableCards = randomCards.Select(card => card.IsMutable ? card : card.ToMutable()).ToList();
        await CardPileCmd.AddGeneratedCardsToCombat(mutableCards, PileType.Hand, player);
        Flash();
    }
}