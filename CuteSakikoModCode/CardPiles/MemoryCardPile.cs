using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Keywords;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Basic;
using Godot;
using HarmonyLib;
using STS2RitsuLib.CardPiles.Nodes;

namespace CuteSakikoMod.CuteSakikoModCode.CardPiles
{
    public sealed class MemoryCardPile
    {
        private static readonly Dictionary<Player, ModCardPile> _cachedByPlayer = new();
        private static readonly HashSet<ModCardPile> _populatingPiles = new();
        // 改用 NetId，避免联机重连时玩家引用变更导致重复填充
        private static readonly HashSet<ulong> _initializedPlayerIds = new();

        [ThreadStatic] private static bool _isAddingSnapshot;

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

        public static ModCardPile? Get(Player player)
        {
            if (_cachedByPlayer.TryGetValue(player, out var cached) && cached != null)
                return cached;
            if (player.PlayerCombatState == null) return null;
            foreach (var pile in player.PlayerCombatState.AllPiles)
            {
                if (pile is ModCardPile mp && mp.Definition.Id.EndsWith("_CARDPILE_MEMORY"))
                {
                    _cachedByPlayer[player] = mp;
                    return mp;
                }
            }
            return null;
        }

        public static async Task PopulateAsync(Player player, ModCardPile pile)
        {
            lock (_populatingPiles)
            {
                if (!_populatingPiles.Add(pile)) return;
                // 双重检查：锁内再次确认牌堆为空，防止并发
                if (pile.Cards.Count > 0) { _populatingPiles.Remove(pile); return; }
            }
            try
            {
                var seenIds = new HashSet<ModelId>(pile.Cards.Select(c => c.Id));
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
            _isAddingSnapshot = true;
            snapshot.AddModKeyword(CutesakiKeywords.Memory);
            _isAddingSnapshot = false;
            snapshot.EnergyCost.SetThisCombat(0, true);
            pile.AddInternal(snapshot);
            seenIds.Add(snapshot.Id);
        }

        public static void Clear()
        {
            _cachedByPlayer.Clear();
            lock (_populatingPiles) { _populatingPiles.Clear(); }
            lock (_initializedPlayerIds) { _initializedPlayerIds.Clear(); }
        }

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

        [HarmonyPatch(typeof(NModCardPileButton), nameof(NModCardPileButton.Initialize))]
        private static class NModCardPileButton_Initialize_Patch
        {
            public static async void Postfix(NModCardPileButton __instance, Player player)
            {
                if (__instance.Definition?.Id.EndsWith("_CARDPILE_MEMORY") != true) return;
                // 使用 NetId 防止重复填充
                lock (_initializedPlayerIds)
                {
                    if (!_initializedPlayerIds.Add(player.NetId)) return;
                }
                var pile = Get(player);
                if (pile == null || pile.Cards.Count > 0) return;
                await PopulateAsync(player, pile);
            }
        }

        [HarmonyPatch(typeof(CardModel), nameof(CardModel.AddKeyword))]
        private static class CardModel_AddKeyword_Patch
        {
            [ThreadStatic] private static bool _isAddingSnapshot;
            public static void Postfix(CardModel __instance, CardKeyword keyword)
            {
                if (_isAddingSnapshot) return;
                if (!ModKeywordRegistry.TryGetCardKeyword(CutesakiKeywords.Memory, out var memoryKeyword)) return;
                if (!keyword.Equals(memoryKeyword)) return;
                if (__instance.Pile is ModCardPile modPile && modPile.Definition.Id.EndsWith("_CARDPILE_MEMORY")) return;
                var player = __instance.Owner;
                if (player == null) return;
                if (__instance.Owner != player) return;
                var memoryPile = Get(player);
                if (memoryPile == null) return;
                if (memoryPile.Cards.Any(c => c.Id == __instance.Id)) return;
                _isAddingSnapshot = true;
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
                finally { _isAddingSnapshot = false; }
            }
        }
    }
}