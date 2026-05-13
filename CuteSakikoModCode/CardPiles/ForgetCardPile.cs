using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Entities.Players;
using STS2RitsuLib.CardPiles;

namespace CuteSakikoMod.CuteSakikoModCode.CardPiles
{
    public sealed class ForgetCardPile
    {
        // 按玩家缓存牌堆实例，避免联机时不同玩家相互污染
        private static readonly Dictionary<Player, ModCardPile> _cachedByPlayer = new();

        public static void Register(string modId)
        {
            var registry = ModCardPileRegistry.For(modId);
            registry.RegisterOwned("Forget", new ModCardPileSpec
            {
                Style = ModCardPileUiStyle.BottomRight,
                IconPath = "res://CuteSakikoMod/images/ui/cardpiles/forget_pile_icon.png",
                VisibleWhen = ctx => ctx.Pile?.Cards.Count > 0,
                Anchor = new ModCardPileAnchor(
                    kind: ModCardPileAnchorKind.BottomRightPrimary, // 注意：遗忘堆应该在右下角
                    offset: new Vector2(98f, 98f)
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
                if (pile is ModCardPile mp && mp.Definition.Id.EndsWith("_CARDPILE_FORGET"))
                {
                    _cachedByPlayer[player] = mp;
                    return mp;
                }
            }
            return null;
        }

        public static void Clear() => _cachedByPlayer.Clear();
    }
}