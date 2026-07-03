using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.Reaction;
using System.Collections.Generic;
using System.IO;

namespace CuteSakikoMod.CuteSakikoModCode.Patches
{
    [HarmonyPatch]
    public static class ReactionPatch
    {
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

        private static readonly Dictionary<string, string> ReverseMap = new();
        static ReactionPatch()
        {
            foreach (var kv in FileMap)
                ReverseMap[kv.Value] = kv.Key;
        }

        private static readonly Dictionary<string, ReactionType> OriginalFileToType = new()
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

        private static readonly Dictionary<string, Texture2D> _cache = new();
        private static bool _wheelPatched = false;

        // ---------- 补丁1：NReaction.Create(Texture2D) ----------
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NReaction), "Create", typeof(Texture2D))]
        public static bool Create_Prefix(ref Texture2D reactionTexture)
        {
            if (reactionTexture == null || reactionTexture.ResourcePath == null)
                return true;

            var fileName = Path.GetFileName(reactionTexture.ResourcePath);
            if (FileMap.TryGetValue(fileName, out var customFile))
            {
                var customPath = $"res://CuteSakikoMod/images/reactions/{customFile}";
                if (!_cache.TryGetValue(customPath, out var customTex))
                {
                    customTex = GD.Load<Texture2D>(customPath);
                    if (customTex != null)
                        _cache[customPath] = customTex;
                }
                if (customTex != null)
                    reactionTexture = customTex;
            }
            return true;
        }

        // ---------- 补丁2：NReaction.TextureToType ----------
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NReaction), "TextureToType")]
        public static bool TextureToType_Prefix(Texture2D texture, ref ReactionType __result)
        {
            if (texture == null || texture.ResourcePath == null)
                return true;

            var fileName = Path.GetFileName(texture.ResourcePath);
            if (ReverseMap.TryGetValue(fileName, out var originalFile))
            {
                if (OriginalFileToType.TryGetValue(originalFile, out var type))
                {
                    __result = type;
                    return false;
                }
            }
            return true;
        }

        // ---------- 补丁3：NReactionWheel._Input ----------
        [HarmonyPrefix] // 使用 Prefix 以便在轮盘显示前处理
        [HarmonyPatch(typeof(NReactionWheel), "_Input")]
        public static void NReactionWheel_Input_Prefix(NReactionWheel __instance, InputEvent inputEvent)
        {
            if (_wheelPatched) return;
            if (inputEvent.IsActionPressed(new StringName("react_wheel")))
            {
                string[] wedgeNames = new[]
                {
                    "RightWedge", "DownRightWedge", "DownWedge", "DownLeftWedge",
                    "LeftWedge", "UpLeftWedge", "UpWedge", "UpRightWedge"
                };

                for (int i = 0; i < wedgeNames.Length; i++)
                {
                    var wedge = __instance.GetNodeOrNull<NReactionWheelWedge>(wedgeNames[i]);
                    if (wedge == null) continue;

                    var textureRect = wedge.GetNodeOrNull<TextureRect>("TextureRect");
                    if (textureRect == null) continue;

                    var originalTex = textureRect.Texture;
                    if (originalTex == null || originalTex.ResourcePath == null) continue;

                    var fileName = Path.GetFileName(originalTex.ResourcePath);
                    if (FileMap.TryGetValue(fileName, out var customFile))
                    {
                        var customPath = $"res://CuteSakikoMod/images/reactions/{customFile}";
                        if (!_cache.TryGetValue(customPath, out var customTex))
                        {
                            customTex = GD.Load<Texture2D>(customPath);
                            if (customTex != null)
                                _cache[customPath] = customTex;
                        }
                        if (customTex != null)
                        {
                            textureRect.Texture = customTex;
                            textureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                            textureRect.StretchMode = TextureRect.StretchModeEnum.Scale;
                            textureRect.CustomMinimumSize = new Vector2(75, 75);
                            textureRect.QueueRedraw();
                        }
                    }
                }
                _wheelPatched = true;
            }
        }
    }
}