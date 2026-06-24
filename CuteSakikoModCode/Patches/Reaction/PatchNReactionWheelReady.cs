using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Reaction;

namespace CuteSakikoMod.CuteSakikoModCode.Patches.Reaction
{
    [HarmonyPatch(typeof(NReactionWheel), nameof(NReactionWheel._Input))]
    public static class Patch_NReactionWheel_Input
    {
        private static bool _patched = false;

        private static readonly string[] WedgeNames = new string[]
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

        private static readonly string[] CustomIcons = new string[]
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

        private static void Postfix(NReactionWheel __instance, InputEvent inputEvent)
        {
            if (_patched) return;

            // 检测轮盘打开动作（按下快捷键）
            if (inputEvent.IsActionPressed(new StringName("react_wheel")))
            {
                ReplaceWedgeIcons(__instance);
                _patched = true;
            }
        }

        private static void ReplaceWedgeIcons(NReactionWheel wheel)
        {
            for (int i = 0; i < WedgeNames.Length; i++)
            {
                var wedge = wheel.GetNodeOrNull<NReactionWheelWedge>(WedgeNames[i]);
                if (wedge == null)
                {
                    GD.PrintErr($"楔子 {WedgeNames[i]} 未找到");
                    continue;
                }

                var tex = GD.Load<Texture2D>(CustomIcons[i]);
                if (tex == null)
                {
                    GD.PrintErr($"纹理 {CustomIcons[i]} 加载失败");
                    continue;
                }

                // ★ 只修改子节点 _textureRect 的纹理（轮盘图标）
                var textureRect = wedge.GetNodeOrNull<TextureRect>("TextureRect");
                if (textureRect != null)
                {
                    textureRect.Texture = tex;
                    textureRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
                    textureRect.StretchMode = TextureRect.StretchModeEnum.Scale;
                    textureRect.CustomMinimumSize = new Vector2(75, 75);
                    textureRect.QueueRedraw();
                }
                else
                {
                    GD.PrintErr($"楔子 {WedgeNames[i]} 缺少 TextureRect 子节点");
                }
            }
        }
    }
}