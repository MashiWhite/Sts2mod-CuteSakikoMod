using System.Reflection;
using CuteSakikoMod.CuteSakikoModCode.Cards.Mutsumi.Planting;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Content;

namespace CuteSakikoMod.CuteSakikoModCode.CardPiles;

/// <summary>
///     种植牌堆（ExtraHand）。放在手牌上方，允许直接打出堆内卡牌；初始容量 3。
///     堆内卡牌是 <see cref="PlantingCardBase" />，由 <see cref="Singletons.PlantingPileManager" /> 在
///     玩家回合开始时推进阶段。
/// </summary>
public sealed class PlantingCardPile
{
    /// <summary>基础种植槽数量；能力/卡牌可通过 <see cref="IPlantingCapacityBonus" /> 增减。</summary>
    public const int BaseCapacity = 3;

    private static readonly Lazy<Func<PileType, Player?, ModCardPile?>> _resolveFunc = new(() =>
    {
        var storageType = typeof(ModCardPileRegistry).Assembly.GetType("STS2RitsuLib.CardPiles.ModCardPileStorage");
        if (storageType == null)
            throw new InvalidOperationException("Cannot find ModCardPileStorage type.");

        var method = storageType.GetMethod("Resolve", BindingFlags.Public | BindingFlags.Static);
        if (method == null)
            throw new InvalidOperationException("Cannot find Resolve method on ModCardPileStorage.");

        return (Func<PileType, Player?, ModCardPile?>)Delegate.CreateDelegate(
            typeof(Func<PileType, Player?, ModCardPile?>), method);
    });

    public static void Register(string modId)
    {
        var registry = ModCardPileRegistry.For(modId);
        registry.RegisterOwned("Planting", new ModCardPileSpec
        {
            Style = ModCardPileUiStyle.ExtraHand,
            Anchor = new ModCardPileAnchor(ModCardPileAnchorKind.ExtraHandAbove),
            IconPath = "res://CuteSakikoMod/images/ui/cardpiles/planting_pile_icon.png",
            CardShouldBeVisible = true,
            ExtraHand = new ModCardPileExtraHandSpec
            {
                AllowCardPlay = true,
                ShowPlayableGlow = true
            },
            // 只有堆里有卡时才显示，避免常驻占位
            VisibleWhen = ctx => ctx.Pile?.Cards.Count > 0
        });
    }

    public static PileType GetPileType()
    {
        var id = ModContentRegistry.GetQualifiedCardPileId(Entry.ModId, "Planting");
        if (!ModCardPileRegistry.TryGet(id, out var definition))
            throw new InvalidOperationException("PlantingCardPile not registered");
        return definition.PileType;
    }

    public static ModCardPile? Get(Player player)
    {
        if (player?.PlayerCombatState == null) return null;

        var id = ModContentRegistry.GetQualifiedCardPileId(Entry.ModId, "Planting");
        if (!ModCardPileRegistry.TryGet(id, out var definition)) return null;

        try
        {
            return _resolveFunc.Value.Invoke(definition.PileType, player);
        }
        catch (Exception ex)
        {
            Log.Error($"[PlantingCardPile] Failed to resolve pile via reflection: {ex.Message}");
            return null;
        }
    }

    public static int Count(Player player) => Get(player)?.Cards.Count ?? 0;

    /// <summary>当前玩家的种植槽上限 = 基础 3 + 所有 <see cref="IPlantingCapacityBonus" /> 的加成。</summary>
    public static int GetCapacity(Player? player)
    {
        var bonus = 0;
        if (player?.Creature != null)
        {
            foreach (var power in player.Creature.Powers)
            {
                if (power is IPlantingCapacityBonus modifier)
                    bonus += modifier.BonusPlantingSlots;
            }
        }
        return Math.Max(0, BaseCapacity + bonus);
    }

    public static bool CanPlant(Player player, int amount) => Count(player) + amount <= GetCapacity(player);

    /// <summary>供其他效果查询：种植堆里的全部种植卡。</summary>
    public static IReadOnlyList<PlantingCardBase> GetPlants(Player player)
    {
        var pile = Get(player);
        if (pile == null) return Array.Empty<PlantingCardBase>();
        return pile.Cards.OfType<PlantingCardBase>().ToList();
    }

    /// <summary>供其他效果查询：处于指定阶段的种植卡（例如「打出所有幼苗阶段的牌」）。</summary>
    public static IReadOnlyList<PlantingCardBase> GetPlantsAtStage(Player player, int stage)
    {
        return GetPlants(player).Where(card => card.Stage == stage).ToList();
    }
}
