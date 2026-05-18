using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons;

[RegisterSingleton]
public sealed class SakiMemoryManager : SingletonModel
{
    private static readonly CardKeyword MemoryKeyword = ModKeywordRegistry.GetCardKeyword(CutesakiKeywords.Memory);

    // 改为按玩家隔离的存储结构
    private readonly Dictionary<Player, HashSet<ModelId>> _exhaustedMemoryIdsByPlayer = new();

    public SakiMemoryManager()
    {
        ModHelper.SubscribeForCombatStateHooks(Id.Entry, _ => [this]);
    }

    public override bool ShouldReceiveCombatHooks => true;

    public static SakiMemoryManager Instance => ModelDb.Singleton<SakiMemoryManager>();

    /// <summary>
    ///     获取指定玩家已消耗的记忆卡 ID 集合（只读）
    /// </summary>
    public IReadOnlyCollection<ModelId> GetExhaustedMemoryIds(Player player)
    {
        if (player == null)
            return Array.Empty<ModelId>();

        if (!_exhaustedMemoryIdsByPlayer.TryGetValue(player, out var set))
            return Array.Empty<ModelId>();

        return set;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Card.Keywords.Contains(MemoryKeyword)) cardPlay.Card.EnergyCost.AddThisCombat(1);
        return Task.CompletedTask;
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card,
        bool causedByEthereal)
    {
        if (!card.Keywords.Contains(MemoryKeyword))
            return;

        var player = card.Owner;
        if (player == null)
            return;

        // 记录到该玩家的已消耗列表
        if (!_exhaustedMemoryIdsByPlayer.TryGetValue(player, out var set))
        {
            set = new HashSet<ModelId>();
            _exhaustedMemoryIdsByPlayer[player] = set;
        }

        set.Add(card.Id);

        // 重放两次（原逻辑）
        for (var i = 0; i < 2; i++)
        {
            var clone = card.CreateClone();
            clone.RemoveKeyword(MemoryKeyword); // 防止递归触发
            clone.ExhaustOnNextPlay = false; // 克隆牌不再消耗
            await CardCmd.AutoPlay(choiceContext, clone, null);

            if (clone.Pile?.IsCombatPile == true)
                await CardPileCmd.RemoveFromCombat(clone);
        }
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _exhaustedMemoryIdsByPlayer.Clear();
        return Task.CompletedTask;
    }
}