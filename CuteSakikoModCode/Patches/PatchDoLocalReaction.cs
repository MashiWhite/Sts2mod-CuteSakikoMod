using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Reaction;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

// 替换本地表情（根据原版纹理路径映射）
[HarmonyPatch(typeof(NReactionContainer), "DoLocalReaction")]
public static class PatchDoLocalReaction
{
    // 原版文件名 → 你的自定义文件名
    private static readonly Dictionary<string, string> FileMap = new()
    {
        ["exclaim.png"] = "tomorin_exclaim.png",
        ["skull.png"] = "mutsumi_skull.png",
        ["thumb_down.png"] = "soyo_thumb_down.png",
        ["slime_sad.png"] = "nyamu_slime_sad.png",
        ["question.png"] = "tomorin_question.png",
        ["heart.png"] = "uika_heart.png",
        ["thumb_up.png"] = "saki_thumb_up.png",
        ["happy_cultist.png"] = "anon_happy_cultist.png"
    };

    private static bool Prefix(NReactionContainer __instance, Texture2D tex, Vector2 position,
        ReactionSynchronizer? ____synchronizer)
    {
        if (tex == null || tex.ResourcePath == null) return true;

        // 从原版文件名映射到自定义路径
        string? customPath = null;
        foreach (var kv in FileMap)
            if (tex.ResourcePath.EndsWith(kv.Key))
            {
                customPath = $"res://CuteSakikoMod/images/reactions/{kv.Value}";
                break;
            }

        if (customPath == null) return true;

        var newTex = GD.Load<Texture2D>(customPath);
        if (newTex == null) return true;

        // 用自定义纹理创建表情
        var child = NReaction.Create(newTex);
        __instance.AddChildSafely(child);
        child.GlobalPosition = position - child.Size / 2f;
        child.BeginAnim();

        // 联机同步：发送一个临时类型（双方都替换了表情，实际显示为自定义图片）
        ____synchronizer?.SendLocalReaction(ReactionType.Heart, position);
        return false; // 跳过原版
    }
}

// 替换远程表情（根据 ReactionType 枚举映射）
[HarmonyPatch(typeof(NReactionContainer), "DoRemoteReaction")]
public static class Patch_DoRemoteReaction
{
    private static readonly Dictionary<ReactionType, string> TypeMap = new()
    {
        { ReactionType.Exclamation, "res://CuteSakikoMod/images/reactions/tomorin_exclaim.png" },
        { ReactionType.Skull, "res://CuteSakikoMod/images/reactions/mutsumi_skull.png" },
        { ReactionType.ThumbDown, "res://CuteSakikoMod/images/reactions/soyo_thumb_down.png" },
        { ReactionType.SadSlime, "res://CuteSakikoMod/images/reactions/nyamu_slime_sad.png" },
        { ReactionType.QuestionMark, "res://CuteSakikoMod/images/reactions/tomorin_question.png" },
        { ReactionType.Heart, "res://CuteSakikoMod/images/reactions/uika_heart.png" },
        { ReactionType.ThumbUp, "res://CuteSakikoMod/images/reactions/saki_thumb_up.png" },
        { ReactionType.HappyCultist, "res://CuteSakikoMod/images/reactions/anon_happy_cultist.png" }
    };

    private static bool Prefix(NReactionContainer __instance, ReactionType type, Vector2 position)
    {
        if (!TypeMap.TryGetValue(type, out var path)) return true;

        var tex = GD.Load<Texture2D>(path);
        if (tex == null) return true;

        var child = NReaction.Create(tex);
        __instance.AddChildSafely(child);
        child.GlobalPosition = position - child.Size / 2f;
        child.BeginAnim();
        return false;
    }

    [HarmonyPatch(typeof(NReactionWheel), "_Ready")]
    public static class Patch_NReactionWheel_Ready
    {
        // 轮盘上 8 个楔子的节点名称（从右侧顺时针）
        private static readonly string[] WedgeNames =
        {
            "RightWedge",
            "DownRightWedge",
            "DownWedge",
            "DownLeftWedge",
            "LeftWedge",
            "UpLeftWedge",
            "UpWedge",
            "UpRightWedge"
        };

        // 对应的自定义图片路径（顺序必须与 WedgeNames 一致）
        private static readonly string[] CustomIcons =
        {
            "res://CuteSakikoMod/images/reactions/tomorin_exclaim.png",
            "res://CuteSakikoMod/images/reactions/mutsumi_skull.png",
            "res://CuteSakikoMod/images/reactions/soyo_thumb_down.png",
            "res://CuteSakikoMod/images/reactions/nyamu_slime_sad.png",
            "res://CuteSakikoMod/images/reactions/tomorin_question.png",
            "res://CuteSakikoMod/images/reactions/uika_heart.png",
            "res://CuteSakikoMod/images/reactions/saki_thumb_up.png",
            "res://CuteSakikoMod/images/reactions/anon_happy_cultist.png"
        };

        private static void Postfix(NReactionWheel __instance)
        {
            for (var i = 0; i < WedgeNames.Length; i++)
            {
                var wedge = __instance.GetNodeOrNull<NReactionWheelWedge>(WedgeNames[i]);
                if (wedge == null) continue;

                var textureRect = wedge.GetNodeOrNull<TextureRect>("TextureRect");
                if (textureRect == null) continue;

                var tex = GD.Load<Texture2D>(CustomIcons[i]);
                if (tex != null)
                {
                    textureRect.Texture = tex;
                    // 保持原版缩放设置
                    textureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                    textureRect.StretchMode = TextureRect.StretchModeEnum.Scale;
                    textureRect.CustomMinimumSize = new Vector2(75, 75);
                }
            }
        }
    }
}