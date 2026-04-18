using System;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Runs;
using Steamworks;
using BaseLib.Abstracts;

namespace CuteSakikoMod.CuteSakikoModCode.Patches
{
    [HarmonyPatch(typeof(RunManager))]
    public static class RichPresencePatch
    {
        private static MethodInfo? _setRichPresenceMethod;
        private static PropertyInfo? _stateProp;

        static RichPresencePatch()
        {
            // 获取 SteamFriends.SetRichPresence 方法
            var steamFriendsType = AccessTools.TypeByName("Steamworks.SteamFriends");
            if (steamFriendsType != null)
            {
                _setRichPresenceMethod = AccessTools.Method(steamFriendsType, "SetRichPresence", new[] { typeof(string), typeof(string) });
            }
            _stateProp = AccessTools.DeclaredProperty(typeof(RunManager), "State");
        }

        [HarmonyPostfix]
        [HarmonyPatch("UpdateRichPresence")]
        public static void UpdateRichPresence_Postfix(RunManager __instance)
        {
            if (__instance == null) return;
            if (_setRichPresenceMethod == null) return;
            if (_stateProp == null) return;

            var state = _stateProp.GetValue(__instance) as RunState;
            if (state == null) return;

            var me = LocalContext.GetMe(state);
            if (me == null) return;

            var character = me.Character;

            if (character is PlaceholderCharacterModel)
            {
                string charName = StripBBCode(character.Title.GetFormattedText());
                string actName = StripBBCode(state.Act.Title.GetFormattedText());
                string customStatus = $"{charName} - {actName} A{state.AscensionLevel}";

                try
                {
                    _setRichPresenceMethod.Invoke(null, new object[] { "Ascension", customStatus });
                    _setRichPresenceMethod.Invoke(null, new object[] { "Character", "REGENT" });
                    _setRichPresenceMethod.Invoke(null, new object[] { "Act", "OVERGROWTH" });
                }
                catch (Exception)
                {
                    // 如果Steam未初始化或任何原因失败，静默忽略
                }
            }
        }

        private static string StripBBCode(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            return System.Text.RegularExpressions.Regex.Replace(input, @"\[.*?\]", "");
        }
    }
}