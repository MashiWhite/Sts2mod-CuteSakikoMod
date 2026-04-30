
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
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
    private readonly HashSet<ModelId> _exhaustedMemoryIds = new();

    public SakiMemoryManager()
    {
        _exhaustedMemoryIds.Clear();
        // 只需要订阅战斗钩子
        ModHelper.SubscribeForCombatStateHooks(Id.Entry, _ => [this]);
    }

    public override bool ShouldReceiveCombatHooks => true;

    public static SakiMemoryManager Instance => ModelDb.Singleton<SakiMemoryManager>();

    // 公开只读集合
    public IReadOnlyCollection<ModelId> ExhaustedMemoryIds => _exhaustedMemoryIds;

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 直接用枚举 Contains，不用 HasModKeyword
        if (cardPlay.Card.Keywords.Contains(MemoryKeyword))
        {
            // 每次打出后，本场战斗永久增加 1 点能量消耗（不是升级基础费用）
            cardPlay.Card.EnergyCost.AddThisCombat(1);
        }
        await Task.CompletedTask;
    }

    public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
    {
        if (card.Keywords.Contains(MemoryKeyword))
        {
            _exhaustedMemoryIds.Add(card.Id);

            // 需要重放两次？循环两次即可
            for (int i = 0; i < 2; i++)
            {
                var clone = card.CreateClone();
                clone.RemoveKeyword(MemoryKeyword);          // 关键：防止再次触发钩子
                clone.ExhaustOnNextPlay = false;             // 不要让克隆再消耗
                await CardCmd.AutoPlay(choiceContext, clone, null, AutoPlayType.Default);
                // 立即清除这张克隆牌（参考 Chord 的做法）
                if (clone.Pile != null && clone.Pile.IsCombatPile)
                    await CardPileCmd.RemoveFromCombat(clone);
            }
        }
    }

    public override async Task AfterCombatEnd(CombatRoom room)
    {
        _exhaustedMemoryIds.Clear();
        await Task.CompletedTask;
    }
}