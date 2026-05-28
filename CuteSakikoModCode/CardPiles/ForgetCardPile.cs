using System.Reflection;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Content;

namespace CuteSakikoMod.CuteSakikoModCode.CardPiles;

public sealed class ForgetCardPile
{
    private static readonly object _lock = new();

    // 缓存反射得到的 ModCardPileStorage.Resolve 委托，避免每次调用都反射
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
        registry.RegisterOwned("Forget", new ModCardPileSpec
        {
            Style = ModCardPileUiStyle.BottomRight,
            IconPath = "res://CuteSakikoMod/images/ui/cardpiles/forget_pile_icon.png",
            VisibleWhen = ctx => ctx.Pile?.Cards.Count > 0,
            Anchor = new ModCardPileAnchor(
                ModCardPileAnchorKind.BottomRightPrimary,
                new Vector2(98f, 98f)
            )
        });
    }

    /// <summary>
    ///     获取或创建遗忘堆实例。通过反射调用 ModCardPileStorage.Resolve 确保懒创建。
    /// </summary>
    public static ModCardPile? Get(Player player)
    {
        if (player?.PlayerCombatState == null) return null;

        var id = ModContentRegistry.GetQualifiedCardPileId("CuteSakikoMod", "Forget");
        if (!ModCardPileRegistry.TryGet(id, out var definition)) return null;

        try
        {
            return _resolveFunc.Value.Invoke(definition.PileType, player);
        }
        catch (Exception ex)
        {
            Log.Error($"[ForgetCardPile] Failed to resolve pile via reflection: {ex.Message}");
            return null;
        }
    }

    public static void Clear()
    {
        // ModCardPileStorage 使用 ConditionalWeakTable，会自动清理
    }
}