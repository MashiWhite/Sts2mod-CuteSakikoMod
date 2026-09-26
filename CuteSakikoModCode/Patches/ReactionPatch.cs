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
            ["exclaim.png"] = "exclaim.png",
            ["skull.png"] = "skull.png",
            ["thumb_down.png"] = "thumb_down.png",
            ["slime_sad.png"] = "slime_sad.png",
            ["question.png"] = "question.png",
            ["heart.png"] = "heart.png",
            ["thumb_up.png"] = "thumb_up.png",
            ["happy_cultist.png"] = "happy_cultist.png"
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
        private static readonly Dictionary<string, Texture2D> _originalWedgeTextures = new();
        private static bool _wheelPatched = false;
        private static bool? _lastAppliedReactionState = null;

        private static readonly string[] WedgeNames =
        {
            "RightWedge", "DownRightWedge", "DownWedge", "DownLeftWedge",
            "LeftWedge", "UpLeftWedge", "UpWedge", "UpRightWedge"
        };

        // ---------- 补丁1：NReaction.Create(Texture2D) ----------
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NReaction), "Create", typeof(Texture2D))]
        public static bool Create_Prefix(ref Texture2D reactionTexture)
        {
            if (!Others.Config.ModConfig.EnableReactionReplacement)
                return true;

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
            if (!Others.Config.ModConfig.EnableReactionReplacement)
                return true;

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
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NReactionWheel), "_Input")]
        public static void NReactionWheel_Input_Prefix(NReactionWheel __instance, InputEvent inputEvent)
        {
            var current = Others.Config.ModConfig.EnableReactionReplacement;

            if (_lastAppliedReactionState != current)
            {
                _wheelPatched = false;
                _lastAppliedReactionState = current;
            }

            // ⭐ 每帧确保 PivotOffset=0 和 Scale 一致
            //    关键：NReactionWheel 场景里 PivotOffset 可能不是 (0,0)，
            //    而我们改 Scale 时如果不把 PivotOffset 归零，Godot 会围绕非左上角缩放，
            //    导致轮盘视觉中心偏移到鼠标位置之外（飘出表情从鼠标位置出发，视觉上就不在轮盘中心）
            var configuredScale = Others.Config.ModConfig.ReactionWheelScale;
            var targetScale = current ? Mathf.Clamp(configuredScale, 0.5f, 3.0f) : 1.0f;

            var scaleNeedsUpdate = !Mathf.IsEqualApprox(__instance.Scale.X, targetScale);
            var pivotNeedsReset = targetScale != 1f && __instance.PivotOffset != Vector2.Zero;

            if (scaleNeedsUpdate || pivotNeedsReset)
            {
                // 归零 PivotOffset，让缩放从左上角扩展，
                // 这样游戏原方法的 GlobalPosition = center - Size * Scale * 0.5f 才能正确居中
                __instance.PivotOffset = Vector2.Zero;
                __instance.Scale = new Vector2(targetScale, targetScale);
                // 不再手动改 GlobalPosition，交给游戏原方法算
            }

            if (!inputEvent.IsActionPressed(new StringName("react_wheel")))
                return;

            if (_wheelPatched) return;

            if (!current)
            {
                RestoreOriginalTextures(__instance);
                _wheelPatched = true;
                return;
            }

            ApplyCustomTextures(__instance);
            _wheelPatched = true;
        }

        private static void ApplyCustomTextures(NReactionWheel wheel)
        {
            foreach (var wedgeName in WedgeNames)
            {
                var wedge = wheel.GetNodeOrNull<NReactionWheelWedge>(wedgeName);
                if (wedge == null) continue;

                var textureRect = wedge.GetNodeOrNull<TextureRect>("TextureRect");
                if (textureRect == null) continue;

                var currentTex = textureRect.Texture;
                if (currentTex == null || currentTex.ResourcePath == null) continue;

                var fileName = Path.GetFileName(currentTex.ResourcePath);
                if (!FileMap.TryGetValue(fileName, out var customFile)) continue;

                if (!_originalWedgeTextures.ContainsKey(wedgeName))
                    _originalWedgeTextures[wedgeName] = currentTex;

                var customPath = $"res://CuteSakikoMod/images/reactions/{customFile}";
                if (!_cache.TryGetValue(customPath, out var customTex))
                {
                    customTex = GD.Load<Texture2D>(customPath);
                    if (customTex != null)
                        _cache[customPath] = customTex;
                }
                if (customTex == null) continue;

                textureRect.Texture = customTex;
                textureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                textureRect.StretchMode = TextureRect.StretchModeEnum.Scale;
                textureRect.CustomMinimumSize = new Vector2(75, 75);
                textureRect.PivotOffset = Vector2.Zero;
                textureRect.Scale = Vector2.One;

                textureRect.QueueRedraw();
            }
        }

        private static void RestoreOriginalTextures(NReactionWheel wheel)
        {
            foreach (var wedgeName in WedgeNames)
            {
                if (!_originalWedgeTextures.TryGetValue(wedgeName, out var originalTex))
                    continue;
                if (originalTex == null) continue;

                var wedge = wheel.GetNodeOrNull<NReactionWheelWedge>(wedgeName);
                if (wedge == null) continue;

                var textureRect = wedge.GetNodeOrNull<TextureRect>("TextureRect");
                if (textureRect == null) continue;

                textureRect.Texture = originalTex;
                textureRect.PivotOffset = Vector2.Zero;
                textureRect.Scale = Vector2.One;

                textureRect.QueueRedraw();
            }
        }

        // ---------- 补丁4：NReaction.Create Postfix，飘出表情放大 ----------
        [HarmonyPostfix]
        [HarmonyPatch(typeof(NReaction), "Create", typeof(Texture2D))]
        public static void Create_Postfix(NReaction __result)
        {
            if (__result == null) return;
            if (!Others.Config.ModConfig.EnableReactionReplacement) return;
            if (__result.Texture == null) return;

            var scale = Others.Config.ModConfig.ReactionEmoteScale;
            if (scale < 0.1f) scale = 0.1f;
            if (scale > 10f) scale = 10f;

            __result.PivotOffset = __result.Size * 0.5f;
            __result.Scale = new Vector2(scale, scale);
        }
    }
}