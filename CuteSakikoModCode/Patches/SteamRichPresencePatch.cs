using System;
using System.Reflection;
using System.Text.RegularExpressions;
using CuteSakikoMod.CuteSakikoModCode.Character;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Rooms;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

/// <summary>
/// 补丁类：自定义 Steam 富文本状态，显示角色名、HP、进阶数及当前房间/遭遇名。
/// 仅当玩家使用 CuteSakiko 角色时生效。
/// </summary>
[HarmonyPatch(typeof(RunManager))]
public static class RichPresencePatch
{
    // 反射缓存
    private static readonly MethodInfo? _setRichPresenceMethod;
    private static readonly PropertyInfo? _stateProp;

    // CuteSakikoCharacter<,,> 的泛型类型定义，用于检测角色类型
    private static readonly Type CuteSakikoCharacterGeneric =
        typeof(CuteSakikoCharacter<,,>).GetGenericTypeDefinition();

    static RichPresencePatch()
    {
        // 获取 SteamFriends.SetRichPresence 方法
        var steamFriendsType = AccessTools.TypeByName("Steamworks.SteamFriends");
        if (steamFriendsType != null)
        {
            _setRichPresenceMethod = AccessTools.Method(steamFriendsType, "SetRichPresence",
                new[] { typeof(string), typeof(string) });
        }

        // 获取 RunManager.State 属性
        _stateProp = AccessTools.DeclaredProperty(typeof(RunManager), "State");
    }

    [HarmonyPostfix]
    [HarmonyPatch("UpdateRichPresence")]
    public static void UpdateRichPresence_Postfix(RunManager __instance)
    {
        // 安全检查
        if (__instance == null) return;
        if (_setRichPresenceMethod == null) return;
        if (_stateProp == null) return;

        // 获取当前 RunState
        var state = _stateProp.GetValue(__instance) as RunState;
        if (state == null) return;

        // 获取本地玩家
        var me = LocalContext.GetMe(state);
        if (me == null) return;

        // 检查是否为 CuteSakiko 角色
        var character = me.Character;
        if (!IsCuteSakikoCharacter(character.GetType())) return;

        // 组装富文本数据
        var charName = StripBBCode(character.Title.GetFormattedText());
        var hpDisplay = $"{me.Creature.CurrentHp}/{me.Creature.MaxHp}";
        var ascension = $"A{state.AscensionLevel}";
        var roomDisplay = GetRoomDisplayName(state);

        var customStatus = $"[{charName}|HP:{hpDisplay}|{ascension}|{roomDisplay}]";

        try
        {
            // 设置 Steam 富文本（保留原始的其他键值）
            _setRichPresenceMethod.Invoke(null, new object[] { "Ascension", customStatus });
            _setRichPresenceMethod.Invoke(null, new object[] { "Character", "REGENT" });
            _setRichPresenceMethod.Invoke(null, new object[] { "Act", "OVERGROWTH" });
        }
        catch
        {
            // Steam 未初始化或调用失败时静默忽略
        }
    }

    /// <summary>
    /// 判断类型是否为 CuteSakikoCharacter<,,> 的派生类。
    /// </summary>
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

    /// <summary>
    /// 移除字符串中的 BBCode 标记（如 [color]...[/color]）。
    /// </summary>
    private static string StripBBCode(string input)
    {
        return string.IsNullOrEmpty(input) ? "" : Regex.Replace(input, @"\[.*?\]", "");
    }

    /// <summary>
    /// 获取当前房间的显示名称。
    /// - 如果是战斗房间，优先显示遭遇标题（如“灰爱音”）；
    /// - 否则显示房间类型的中文名称（休息、商店等）。
    /// </summary>
    private static string GetRoomDisplayName(RunState state)
    {
        var room = state.CurrentRoom;
        if (room == null) return "卖萌中";

        // 战斗房间：显示遭遇名称
        if (room is CombatRoom combatRoom)
        {
            var encounter = combatRoom.Encounter;
            if (encounter != null && encounter.Title.Exists())
            {
                return StripBBCode(encounter.Title.GetFormattedText());
            }
            // 遭遇标题不存在时，回退到房间类型
        }

        // 非战斗房间：根据 RoomType 返回中文名称
        return room.RoomType switch
        {
            RoomType.Monster => "战斗",
            RoomType.Elite   => "精英",
            RoomType.Boss    => "首领",
            RoomType.Event   => "事件",
            RoomType.RestSite=> "休息",
            RoomType.Shop    => "商店",
            RoomType.Treasure=> "宝箱",
            _                => room.RoomType.ToString()
        };
    }
}