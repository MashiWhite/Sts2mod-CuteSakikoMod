using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Runs;
using STS2RitsuLib.Scaffolding.Characters;

// 新增

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(RunManager))]
public static class RichPresencePatch
{
    private static readonly MethodInfo? _setRichPresenceMethod;
    private static readonly PropertyInfo? _stateProp;

    private static readonly Type ModCharacterTemplateGeneric =
        typeof(ModCharacterTemplate<,,>).GetGenericTypeDefinition();

    static RichPresencePatch()
    {
        // 获取 SteamFriends.SetRichPresence 方法
        var steamFriendsType = AccessTools.TypeByName("Steamworks.SteamFriends");
        if (steamFriendsType != null)
            _setRichPresenceMethod = AccessTools.Method(steamFriendsType, "SetRichPresence",
                new[] { typeof(string), typeof(string) });
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

        // 检查是否为 ModCharacterTemplate<,,> 的派生类
        if (IsModCharacterTemplate(character.GetType()))
        {
            var charName = StripBBCode(character.Title.GetFormattedText());
            var actName = StripBBCode(state.Act.Title.GetFormattedText());
            var customStatus = $"{charName} - {actName} A{state.AscensionLevel}";

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

    private static bool IsModCharacterTemplate(Type type)
    {
        while (type != null && type != typeof(object))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == ModCharacterTemplateGeneric)
                return true;
            type = type.BaseType;
        }

        return false;
    }

    private static string StripBBCode(string input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return Regex.Replace(input, @"\[.*?\]", "");
    }
}