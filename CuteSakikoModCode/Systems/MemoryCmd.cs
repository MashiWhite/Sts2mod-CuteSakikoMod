using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using CuteSakikoMod.CuteSakikoModCode.CardPiles;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Singletons;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.CardPiles;

namespace CuteSakikoMod.CuteSakikoModCode.Systems
{
    public static class MemoryCmd
    {
        /// <summary>
        /// 将卡牌遗忘（移入 ForgetCardPile 或消耗堆），并消耗压力，然后触发事件。
        /// 所有操作均使用官方同步命令，确保联机状态一致。
        /// </summary>
        public static async Task Forget(PlayerChoiceContext choiceContext, IEnumerable<CardModel> cards, CardModel? source = null, bool removeFromMemory = true)
        {
            var list = cards.ToList();
            if (list.Count == 0) return;
            var player = list[0].Owner;

            var forgetPile = ForgetCardPile.Get(player);
            if (forgetPile == null)
            {
                Log.Error($"[MemoryCmd] ForgetCardPile is null for player {player.NetId}");
                return;
            }

            PileType targetPileType = forgetPile.Type;
            var memoryPile = removeFromMemory ? MemoryCardPile.Get(player) : null;

            foreach (var card in list)
            {
                if (card.Pile?.Type == targetPileType) continue;

                await CardPileCmd.Add(card, targetPileType);

                var pressure = player.Creature.GetPower<PressurePower>();
                if (pressure != null)
                    await PowerCmd.ModifyAmount(choiceContext, pressure, -2, player.Creature, source);

                if (memoryPile != null && removeFromMemory)
                {
                    var toRemove = memoryPile.Cards.Where(c => c.Id == card.Id).ToList();
                    foreach (var mCard in toRemove)
                        memoryPile.RemoveInternal(mCard, silent: false); // 改这里
                }
            }

            await MemoryCardPileManager.FireOnForgottenCards(choiceContext, list, source);
        }

        public static List<CardModel> Recall(PlayerChoiceContext choiceContext, Player player, int count, bool upgraded, CardModel source = null)
        {
            var memoryPile = MemoryCardPile.Get(player);
            if (memoryPile == null || memoryPile.Cards.Count == 0)
                return new List<CardModel>();
            var rng = player.RunState.Rng.Shuffle;
            var available = memoryPile.Cards.ToList();
            if (count > available.Count) count = available.Count;
            var chosen = new List<CardModel>();
            var tempList = available.ToList();
            for (int i = 0; i < count; i++)
            {
                int index = rng.NextInt(0, tempList.Count);
                chosen.Add(tempList[index]);
                tempList.RemoveAt(index);
            }
            var newCards = new List<CardModel>();
            foreach (var template in chosen)
            {
                var clone = player.RunState.CloneCard(template);
                if (upgraded && !clone.IsUpgraded)
                    clone.UpgradeInternal();
                newCards.Add(clone);
            }
            if (newCards.Count > 0)
            {
                var handPile = player.PlayerCombatState?.Hand;
                if (handPile != null)
                {
                    foreach (var c in newCards)
                        handPile.AddInternal(c, silent: true);
                    handPile.InvokeContentsChanged();
                }
            }
            return newCards;
        }
    }
}