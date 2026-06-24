using System;
using System.Reflection;
using System.Text.RegularExpressions;
using CuteSakikoMod.CuteSakikoModCode.Character;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

[HarmonyPatch(typeof(RunManager))]
public static class RichPresencePatch
{
    private static readonly MethodInfo? _setRichPresenceMethod;
    private static readonly PropertyInfo? _stateProp;
    private static readonly Type CuteSakikoCharacterGeneric =
        typeof(CuteSakikoCharacter<,,>).GetGenericTypeDefinition();

    // RoomType → map.json 本地化键映射
    private static readonly Dictionary<RoomType, string> RoomTypeLocKeys = new()
    {
        { RoomType.Event, "LEGEND_EVENT.title" },
        { RoomType.RestSite, "LEGEND_REST.title" },
        { RoomType.Shop, "LEGEND_MERCHANT.title" },
        { RoomType.Treasure, "LEGEND_TREASURE.title" },
    };

    // MapPointType → map.json 本地化键映射
    private static readonly Dictionary<MapPointType, string> MapPointTypeLocKeys = new()
    {
        { MapPointType.Unknown, "LEGEND_UNKNOWN.title" },
        { MapPointType.Shop, "LEGEND_MERCHANT.title" },
        { MapPointType.Treasure, "LEGEND_TREASURE.title" },
        { MapPointType.RestSite, "LEGEND_REST.title" },
    };

    static RichPresencePatch()
    {
        var steamFriendsType = AccessTools.TypeByName("Steamworks.SteamFriends");
        if (steamFriendsType != null)
        {
            _setRichPresenceMethod = AccessTools.Method(steamFriendsType, "SetRichPresence",
                new[] { typeof(string), typeof(string) });
        }
        _stateProp = AccessTools.DeclaredProperty(typeof(RunManager), "State");
    }

    [HarmonyPostfix]
    [HarmonyPatch("UpdateRichPresence")]
    public static void UpdateRichPresence_Postfix(RunManager __instance)
    {
        var state = _stateProp.GetValue(__instance) as RunState;
        if (state != null)
            SetRichPresence(state);
    }

    [HarmonyPostfix]
    [HarmonyPatch("EnterRoomInternal", new Type[] { typeof(AbstractRoom), typeof(bool) })]
    public static void EnterRoomInternal_Postfix(RunManager __instance)
    {
        var state = __instance.DebugOnlyGetState();
        if (state != null)
            SetRichPresence(state);
    }

    private static void SetRichPresence(RunState state)
    {
        if (state == null || _setRichPresenceMethod == null) return;

        var me = LocalContext.GetMe(state);
        if (me == null) return;

        var character = me.Character;
        if (!IsCuteSakikoCharacter(character.GetType())) return;

        var charName = StripBBCode(character.Title.GetFormattedText());
        var hpDisplay = $"{me.Creature.CurrentHp}/{me.Creature.MaxHp}";
        var ascension = $"A{state.AscensionLevel}";
        var floor = state.ActFloor + 1;
        var roomDisplay = GetRoomDisplayName(state) ?? "卖萌中";

        var customStatus = $"[{charName}|HP:{hpDisplay}|{ascension}|第{floor}层|{roomDisplay}]";

        try
        {
            _setRichPresenceMethod.Invoke(null, new object[] { "Ascension", customStatus });
            _setRichPresenceMethod.Invoke(null, new object[] { "Character", "REGENT" });
            _setRichPresenceMethod.Invoke(null, new object[] { "Act", "OVERGROWTH" });
        }
        catch { }
    }

    private static bool IsCuteSakikoCharacter(Type type)
    {
        while (type != null && type != typeof(object))
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == CuteSakikoCharacterGeneric)
                return true;
            type = type.BaseType;
        }
        return false;
    }

    private static string StripBBCode(string input)
    {
        return string.IsNullOrEmpty(input) ? "" : Regex.Replace(input, @"\[.*?\]", "");
    }

    private static string? GetLocalizedText(string table, string key)
    {
        var loc = new LocString(table, key);
        if (loc.Exists())
            return StripBBCode(loc.GetFormattedText());
        return null;
    }

    private static string? GetRoomDisplayName(RunState state)
    {
        var room = state.CurrentRoom;
        if (room != null)
        {
            var name = GetRoomNameFromRoom(room);
            if (name != null)
                return name;
        }

        room = state.BaseRoom;
        if (room != null)
        {
            var name = GetRoomNameFromRoom(room);
            if (name != null)
                return name;
        }

        var point = state.CurrentMapPoint;
        if (point != null)
        {
            // 战斗/精英/首领/先古：没有具体名称，返回 null
            if (point.PointType == MapPointType.Monster ||
                point.PointType == MapPointType.Elite ||
                point.PointType == MapPointType.Boss ||
                point.PointType == MapPointType.Ancient)
                return null;

            if (MapPointTypeLocKeys.TryGetValue(point.PointType, out var key))
                return GetLocalizedText("map", key);
        }

        return null;
    }

    private static string? GetRoomNameFromRoom(AbstractRoom room)
    {
        if (room is CombatRoom combatRoom)
        {
            var encounter = combatRoom.Encounter;
            if (encounter != null)
            {
                var title = encounter.Title;
                if (title.Exists())
                    return StripBBCode(title.GetFormattedText());
            }
            return null;
        }

        if (RoomTypeLocKeys.TryGetValue(room.RoomType, out var key))
            return GetLocalizedText("map", key);

        return null;
    }
}