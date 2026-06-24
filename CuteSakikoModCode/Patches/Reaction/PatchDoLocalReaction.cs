using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Reaction;

namespace CuteSakikoMod.CuteSakikoModCode.Patches.Reaction;

[HarmonyPatch(typeof(NReactionContainer), "DoLocalReaction")]
public static class PatchDoLocalReaction
{
    // 原版文件名 → ReactionType 映射（用于发送同步类型）
    private static readonly Dictionary<string, ReactionType> FileToType = new()
    {
        ["exclaim.png"] = ReactionType.Exclamation,
        ["skull.png"] = ReactionType.Skull,
        ["thumb_down.png"] = ReactionType.ThumbDown,
        ["slime_sad.png"] = ReactionType.SadSlime,
        ["question.png"] = ReactionType.QuestionMark,
        ["heart.png"] = ReactionType.Heart,
        ["thumb_up.png"] = ReactionType.ThumbUp,
        ["happy_cultist.png"] = ReactionType.HappyCultist
    };

    // 原版文件名 → 自定义文件名（用于本地显示）
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

        // 1. 获取原版文件名
        var fileName = System.IO.Path.GetFileName(tex.ResourcePath);
        if (string.IsNullOrEmpty(fileName)) return true;

        // 2. 确定 ReactionType（用于同步）
        if (!FileToType.TryGetValue(fileName, out var reactionType))
            return true; // 未知类型，交给原版处理

        // 3. 获取自定义纹理路径
        if (!FileMap.TryGetValue(fileName, out var customFileName))
            return true; // 没有映射，交给原版

        var customPath = $"res://CuteSakikoMod/images/reactions/{customFileName}";
        var newTex = GD.Load<Texture2D>(customPath);
        if (newTex == null) return true;

        // 4. 用自定义纹理创建表情（本地显示）
        var child = NReaction.Create(newTex);
        __instance.AddChildSafely(child);
        child.GlobalPosition = position - child.Size / 2f;
        child.BeginAnim();

        // 5. 发送同步消息，使用正确的 ReactionType
        ____synchronizer?.SendLocalReaction(reactionType, position);

        return false; // 跳过原版
    }
}