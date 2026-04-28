using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki;
using CuteSakikoMod.CuteSakikoModCode.Others;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace CuteSakikoMod.CuteSakikoModCode.Singletons
{
    public class SakiMemoryManager : CustomSingletonModel
    {
        private static readonly HashSet<ModelId> _exhaustedMemoryIds = new();

        public static IReadOnlyCollection<ModelId> ExhaustedMemoryIds => _exhaustedMemoryIds;

        public SakiMemoryManager() : base(true, true)
        {
            _exhaustedMemoryIds.Clear();
        }

        // 手动打出回忆牌 → 永久涨费
        public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
        {
            if (cardPlay.Card.CanonicalKeywords.Contains(CutesakiKeywords.Memory))
            {
                cardPlay.Card.EnergyCost.UpgradeBy(1);
            }
            await Task.CompletedTask;
        }

        // 被消耗时 → 触发两次打出效果，并移出可用池
        public override async Task AfterCardExhausted(
            PlayerChoiceContext choiceContext,
            CardModel card,
            bool causedByEthereal)
        {
            if (card.CanonicalKeywords.Contains(CutesakiKeywords.Memory))
            {
                if (card is SakiMemoryCard customCard)
                {
                    // 统一调用两次，次数不再由卡牌内部控制
                    await customCard.ProcessMemoryEffect(choiceContext);
                    await customCard.ProcessMemoryEffect(choiceContext);
                }
                _exhaustedMemoryIds.Add(card.Id);
            }
        }

        // 战斗结束清空集合
        public override async Task AfterCombatEnd(CombatRoom room)
        {
            _exhaustedMemoryIds.Clear();
            await Task.CompletedTask;
        }
    }
}