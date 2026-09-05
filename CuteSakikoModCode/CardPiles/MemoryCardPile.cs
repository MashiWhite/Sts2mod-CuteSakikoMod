using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Others;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Starter;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.CardPiles.Nodes;
using STS2RitsuLib.Content;
using STS2RitsuLib.Keywords;

namespace CuteSakikoMod.CuteSakikoModCode.CardPiles;

public sealed class MemoryCardPile
{
    private static readonly Dictionary<Player, ModCardPile> _cachedByPlayer = new();
    private static readonly HashSet<ModCardPile> _populatingPiles = new();
    private static readonly HashSet<ulong> _uiInitializedPlayerIds = new();
    internal static bool _isAddingSnapshot;

    private static readonly Lazy<Func<PileType, Player?, ModCardPile?>> _resolveFunc = new(() =>
    {
        var storageType = typeof(ModCardPileRegistry).Assembly.GetType("STS2RitsuLib.CardPiles.ModCardPileStorage");
        if (storageType == null)
            throw new InvalidOperationException("Cannot find ModCardPileStorage type.");
        var method = storageType.GetMethod("Resolve", BindingFlags.Public | BindingFlags.Static);
        if (method == null)
            throw new InvalidOperationException("Cannot find Resolve method.");
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
            Anchor = new ModCardPileAnchor(ModCardPileAnchorKind.BottomLeftPrimary, new Vector2(-98f, -98f))
        });
    }

    public static ModCardPile? Get(Player player)
    {
        if (player?.PlayerCombatState == null) return null;
        if (_cachedByPlayer.TryGetValue(player, out var cached) && cached != null)
            return cached;
        var id = ModContentRegistry.GetQualifiedCardPileId("CuteSakikoMod", "Memory");
        if (!ModCardPileRegistry.TryGet(id, out var definition)) return null;
        ModCardPile? pile = null;
        try
        {
            pile = _resolveFunc.Value.Invoke(definition.PileType, player);
        }
        catch (Exception ex)
        {
            Log.Error($"[MemoryCardPile] Failed to resolve pile: {ex.Message}");
            return null;
        }
        if (pile != null) _cachedByPlayer[player] = pile;
        return pile;
    }

    public static async Task PopulateAsync(Player player, ModCardPile pile)
    {
        lock (_populatingPiles)
        {
            if (!_populatingPiles.Add(pile)) return;
        }
        try
        {
            var cardsToRemove = pile.Cards.ToList();
            foreach (var c in cardsToRemove)
                pile.RemoveInternal(c, true);

            var seenIds = new HashSet<ModelId>();
            var count = 0;
            // 关键：使用 Ordinal 排序，确保所有客户端顺序一致
            var allMemoryCards = ModelDb.AllCards
                .Where(c => c.Keywords.Contains(CutesakiKeywords.Memory.GetModCardKeyword()))
                .OrderBy(c => c.Id.Entry, StringComparer.Ordinal)
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
            lock (_populatingPiles)
            {
                _populatingPiles.Remove(pile);
            }
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
        pile.AddInternal(snapshot);
        seenIds.Add(snapshot.Id);

        _isAddingSnapshot = true;

        // 1. 设置费用为 1
        snapshot.EnergyCost.SetThisCombat(1, true);

        // 2. 添加 Memory 关键词
        snapshot.AddKeyword(CutesakiKeywords.Memory.GetModCardKeyword());

        // 3. 如果源卡有附魔，克隆并应用到快照
        if (source.Enchantment != null)
        {
            var enchantClone = source.Enchantment.ClonePreservingMutability() as EnchantmentModel;
            if (enchantClone != null)
            {
                snapshot.EnchantInternal(enchantClone, enchantClone.Amount);
                // 附魔可能会修改费用，我们强制保持 1 费
                snapshot.EnergyCost.SetThisCombat(1, true);
            }
        }

        _isAddingSnapshot = false;
    }

    public static void Clear()
    {
        _cachedByPlayer.Clear();
        lock (_populatingPiles) { _populatingPiles.Clear(); }
        lock (_uiInitializedPlayerIds) { _uiInitializedPlayerIds.Clear(); }
    }

    public static List<CardModel> GetCanonicalCards(Player player)
    {
        var pile = Get(player);
        if (pile == null || pile.Cards.Count == 0)
            return new List<CardModel>();

        // 直接返回快照列表，按 ID 排序保证顺序一致
        return pile.Cards
            .OrderBy(c => c.Id.Entry, StringComparer.Ordinal)
            .ToList();
    }
    
    public static CardModel CreateCardFromMemorySnapshot(Player owner, CardModel snapshot)
    {
        if (snapshot == null || owner == null) return null;

        var canonical = ModelDb.GetById<CardModel>(snapshot.Id);
        if (canonical == null) return null;

        var combatState = owner.Creature?.CombatState;
        if (combatState == null) return null;

        var card = combatState.CreateCard(canonical, owner);
        if (card == null) return null;

        // 复制费用
        card.EnergyCost.SetThisCombat(1, true);

        // 复制关键词
        foreach (var kw in snapshot.Keywords)
            if (!card.Keywords.Contains(kw))
                card.AddKeyword(kw);

        // 复制升级状态
        if (snapshot.IsUpgraded && !card.IsUpgraded)
        {
            card.UpgradeInternal();
            card.FinalizeUpgradeInternal();
        }

        // 复制附魔
        if (snapshot.Enchantment != null)
        {
            var enchantClone = snapshot.Enchantment.ClonePreservingMutability() as EnchantmentModel;
            if (enchantClone != null)
            {
                card.EnchantInternal(enchantClone, enchantClone.Amount);
                // 注意：EnchantInternal 会调用 ApplyInternal，可能会修改卡牌属性，比如费用、关键词等，这些应该已经被复制了，但为了保险，可以再次设置费用为0？
                // 因为附魔可能会覆盖费用，但我们的卡期望是1费，所以重新设置一下
                card.EnergyCost.SetThisCombat(1, true);
            }
        }

        return card;
    }

    public static async Task EnsureInitializedAsync(Player player)
    {
        var pile = Get(player);
        if (pile == null) return;
        if (pile.Cards.Count > 0) return;
        await PopulateAsync(player, pile);
    }

    // UI 按钮补丁：不再触发填充，只记录已初始化
    [HarmonyPatch(typeof(NModCardPileButton), nameof(NModCardPileButton.Initialize))]
    private static class NModCardPileButton_Initialize_Patch
    {
        public static void Postfix(NModCardPileButton __instance, Player player)
        {
            if (__instance.Definition?.Id.EndsWith("_CARDPILE_MEMORY") != true) return;
            lock (_uiInitializedPlayerIds)
            {
                _uiInitializedPlayerIds.Add(player.NetId);
            }
        }
    }

    [HarmonyPatch(typeof(CardModel), nameof(CardModel.AddKeyword))]
    private static class CardModel_AddKeyword_Patch
    {
        [ThreadStatic] private static bool _isAddingSnapshotLocal;

        public static void Postfix(CardModel __instance, CardKeyword keyword)
        {
            if (_isAddingSnapshot) return;
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
                    snapshot.AddKeyword(CutesakiKeywords.Memory.GetModCardKeyword());
                    snapshot.EnergyCost.SetThisCombat(1, true);
                    memoryPile.AddInternal(snapshot);
                    memoryPile.InvokeCardAddFinished();
                }
            }
            finally
            {
                _isAddingSnapshotLocal = false;
            }
        }
    }
}