using HarmonyLib;
using Godot;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Entities.Players;
using System.Linq;
using MegaCrit.Sts2.Core.Extensions;
using BaseLib.Abstracts; // 引入 PlaceholderCharacterModel 所在的命名空间

namespace CuteSakikoMod.CuteSakikoModCode.Patches
{
    [HarmonyPatch(typeof(NRestSiteCharacter))]
    public static class RestSiteCharacterFlipPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(NRestSiteCharacter._Ready))]
        public static void Postfix(NRestSiteCharacter __instance)
        {
            Player player = __instance.Player;
            if (player == null) return;

            // 只对继承自 PlaceholderCharacterModel 的自定义角色生效
            if (!(player.Character is PlaceholderCharacterModel)) return;

            var players = player.RunState?.Players;
            if (players == null) return;
            int index = players.IndexOf(player);
            if (index < 0) return;

            // 索引为奇数（1,3）时翻转，偶数（0,2）不翻转
            if (index % 2 == 1)
            {
                Node2D sprite = __instance.GetNodeOrNull<Node2D>("Sprite2D");
                if (sprite != null)
                {
                    sprite.Scale = new Vector2(-Mathf.Abs(sprite.Scale.X), sprite.Scale.Y);
                }
            }
        }
    }
}