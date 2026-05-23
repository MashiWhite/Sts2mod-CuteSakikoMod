using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Keywords;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Basic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Content;

namespace CuteSakikoMod.CuteSakikoModCode.CardPiles
{
    public sealed class MemoryCardPile
    {
        private static readonly Dictionary<Player, ModCardPile> _cachedByPlayer = new();
        private static readonly HashSet<ModCardPile> _populatingPiles = new();
        // 仅用于 UI 按钮补丁的初始化标记，战斗初始化补丁已不再依赖它
        private static readonly HashSet<ulong> _uiInitializedPlayerIds = new();

        internal static bool _isAddingSnapshot;

        // 反射调用 ModCardPileStorage.Resolve 的委托缓存，确保即使 UI 未初始化也能拿到牌堆实例
        private static readonly Lazy<Func<PileType, Player?, ModCardPile?>> _resolveFunc = new(() =>
        {
            var storageType = typeof(ModCardPileRegistry).Assembly.GetType("STS2RitsuLib.CardPiles.ModCardPileStorage");
            if (storageType == null)
                throw new InvalidOperationException("Cannot find ModCardPileStorage type. RitsuLib may have changed.");

            var method = storageType.GetMethod("Resolve", BindingFlags.Public | BindingFlags.Static);
            if (method == null)
                throw new InvalidOperationException("Cannot find Resolve method on ModCardPileStorage.");

            return (Func<PileType, Player?, ModCardPile?>)Delegate.CreateDelegate(
                typeof(Func<PileType, Player?, ModCardPile?>), method);
        });

        public static void Register(string modId)
        {
            var registry = ModCardPileRegistry.For(modId);
            registry.RegisterOwned("Memory", new ModCardPileSpec
            {
                Style = ModCardPileUiStyle.BottomLeft,
                IconPath = "res://CuteSakikoMod/images/ui/cardpiles/memory_pile_icon.png",
                VisibleWhen = ctx => ctx.Player?.GetRelic<KabutoNote>() != null,
                Anchor = new ModCardPileAnchor(
                    kind: ModCardPileAnchorKind.BottomLeftPrimary,
                    offset: new Vector2(-98f, -98f)
                )
            });
        }

        /// <summary>
        /// 获取或创建记忆牌堆实例。优先使用缓存，否则通过反射强制创建。
        /// </summary>
        public static ModCardPile? Get(Player player)
        {
            if (player?.PlayerCombatState == null) return null;

            // 先检查缓存
            if (_cachedByPlayer.TryGetValue(player, out var cached) && cached != null)
                return cached;

            // 通过已注册的定义获取 PileType，然后调用 RitsuLib 内部 Resolve 强制创建
            var id = ModContentRegistry.GetQualifiedCardPileId("CuteSakikoMod", "Memory");
            if (!ModCardPileRegistry.TryGet(id, out var definition)) return null;

            ModCardPile? pile = null;
            try
            {
                pile = _resolveFunc.Value.Invoke(definition.PileType, player);
            }
            catch (Exception ex)
            {
                Log.Error($"[MemoryCardPile] Failed to resolve pile via reflection: {ex.Message}");
                return null;
            }

            if (pile != null)
            {
                _cachedByPlayer[player] = pile;
            }
            return pile;
        }

        /// <summary>
        /// 填充牌堆，内部有锁防止并发填充。
        /// </summary>
        public static async Task PopulateAsync(Player player, ModCardPile pile)
        {
            lock (_populatingPiles)
            {
                if (!_populatingPiles.Add(pile)) return;
            }
            try
            {
                // 先清空牌堆，防止旧缓存重复
                var cardsToRemove = pile.Cards.ToList();
                foreach (var c in cardsToRemove)
                    pile.RemoveInternal(c, silent: true);

                var seenIds = new HashSet<ModelId>();
                int count = 0;
                var allMemoryCards = ModelDb.AllCards
                    .Where(c => c.HasModKeyword(CutesakiKeywords.Memory))
                    .OrderBy(c => c.Id.ToString())
                    .ToList();
                foreach (var template in allMemoryCards)
                {
                    if (!seenIds.Contains(template.Id))
                    {
                        AddSnapshot(player, pile, template, seenIds);
                        count++;
                        if (count % 10 == 0) await Task.Yield();
                    }
                }
                pile.InvokeCardAddFinished();
            }
            finally
            {
                lock (_populatingPiles) { _populatingPiles.Remove(pile); }
            }
        }

        /// <summary>
        /// 动态添加单张卡牌到记忆牌堆（当卡牌被赋予记忆关键词时触发）。
        /// </summary>
        public static void AddSingleCard(Player player, CardModel card)
        {
            if (card.Owner != player) return;
            var pile = Get(player);
            if (pile == null) return;
            if (pile.Cards.Any(c => c.Id == card.Id)) return;
            var seenIds = new HashSet<ModelId>(pile.Cards.Select(c => c.Id));
            AddSnapshot(player, pile, card, seenIds);
            pile.InvokeCardAddFinished();
        }

        private static void AddSnapshot(Player player, ModCardPile pile, CardModel source, HashSet<ModelId> seenIds)
        {
            if (seenIds.Contains(source.Id)) return;
            var template = ModelDb.GetById<CardModel>(source.Id);
            if (template == null) return;
            var snapshot = player.RunState.CreateCard(template, player);

            // 先无声加入牌堆，再添加关键词，避免补丁误判重复
            pile.AddInternal(snapshot);
            seenIds.Add(snapshot.Id);

            _isAddingSnapshot = true;
            snapshot.AddModKeyword(CutesakiKeywords.Memory);
            _isAddingSnapshot = false;

            snapshot.EnergyCost.SetThisCombat(0, true);
        }

        /// <summary>
        /// 清理所有缓存和状态，通常在战斗结束时调用。
        /// </summary>
        public static void Clear()
        {
            _cachedByPlayer.Clear();
            lock (_populatingPiles) { _populatingPiles.Clear(); }
            lock (_uiInitializedPlayerIds) { _uiInitializedPlayerIds.Clear(); }
        }

        /// <summary>
        /// 获取记忆牌堆中所有卡牌的规范模板列表，用于随机抽取等操作。
        /// </summary>
        public static List<CardModel> GetCanonicalCards(Player player)
        {
            var pile = Get(player);
            if (pile == null || pile.Cards.Count == 0) return new List<CardModel>();
            var result = new List<CardModel>();
            foreach (var card in pile.Cards)
            {
                var template = ModelDb.GetById<CardModel>(card.Id);
                if (template != null) result.Add(template);
            }
            return result;
        }

        /// <summary>
        /// 安全初始化：如果牌堆为空，则填充；否则直接返回。
        /// 战斗开始时（通过 AfterRoomEntered 补丁）调用，确保记忆牌堆有数据。
        /// </summary>
        public static async Task EnsureInitializedAsync(Player player)
        {
            var pile = Get(player);
            if (pile == null) return;

            // 只要牌堆为空，就填充。这符合 CombatOnly 设计：每场战斗开始时填充。
            if (pile.Cards.Count > 0) return;

            await PopulateAsync(player, pile);
        }

        // UI 按钮初始化补丁：当记忆牌堆按钮首次创建时，若牌堆为空则填充。
        // 使用独立的 _uiInitializedPlayerIds 避免与战斗初始化补丁冲突。
        [HarmonyPatch(typeof(NModCardPileButton), nameof(NModCardPileButton.Initialize))]
        private static class NModCardPileButton_Initialize_Patch
        {
            public static async void Postfix(NModCardPileButton __instance, Player player)
            {
                if (__instance.Definition?.Id.EndsWith("_CARDPILE_MEMORY") != true) return;
                lock (_uiInitializedPlayerIds)
                {
                    if (!_uiInitializedPlayerIds.Add(player.NetId)) return;
                }
                var pile = Get(player);
                if (pile == null || pile.Cards.Count > 0) return;
                await PopulateAsync(player, pile);
            }
        }

        // 关键词补丁：当卡牌被赋予记忆关键词时，自动添加到记忆牌堆。
        [HarmonyPatch(typeof(CardModel), nameof(CardModel.AddKeyword))]
        private static class CardModel_AddKeyword_Patch
        {
            [ThreadStatic] private static bool _isAddingSnapshotLocal;
            public static void Postfix(CardModel __instance, CardKeyword keyword)
            {
                if (MemoryCardPile._isAddingSnapshot) return;
                if (_isAddingSnapshotLocal) return;
                if (!ModKeywordRegistry.TryGetCardKeyword(CutesakiKeywords.Memory, out var memoryKeyword)) return;
                if (!keyword.Equals(memoryKeyword)) return;
                if (__instance.Pile is ModCardPile modPile && modPile.Definition.Id.EndsWith("_CARDPILE_MEMORY")) return;
                var player = __instance.Owner;
                if (player == null) return;
                var memoryPile = Get(player);
                if (memoryPile == null) return;
                if (memoryPile.Cards.Any(c => c.Id == __instance.Id)) return;
                _isAddingSnapshotLocal = true;
                try
                {
                    var template = ModelDb.GetById<CardModel>(__instance.Id);
                    if (template != null)
                    {
                        var snapshot = player.RunState.CreateCard(template, player);
                        snapshot.AddModKeyword(CutesakiKeywords.Memory);
                        snapshot.EnergyCost.SetThisCombat(0, true);
                        memoryPile.AddInternal(snapshot);
                        memoryPile.InvokeCardAddFinished();
                    }
                }
                finally { _isAddingSnapshotLocal = false; }
            }
        }
    }
}