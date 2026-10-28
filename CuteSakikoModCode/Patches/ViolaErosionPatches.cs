using System.Runtime.CompilerServices;
using CuteSakikoMod.CuteSakikoModCode.Others.Afflictions;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace CuteSakikoMod.CuteSakikoModCode.Patches;

/// <summary>
///     欺诈之蛇：被侵蚀的卡牌在卡面上变成"无法辨认"。
///     三个只读 postfix：卡图 → viola、标题与描述 → 持续变化的乱码。
///     乱码按当前语言取字符池（中：汉字 / 日：假名 / 英：字母），由 FraudulentSerpentPower 的定时器刷新。
/// </summary>
internal static class ViolaErosionText
{
    public const string CardImagePath = "res://CuteSakikoMod/images/cards/viola.png";
    public const string GarbledTitleKey = "CUTE_SAKIKO_MOD_CARD_VIOLA_GARBLED.title";
    public const string GarbledDescriptionKey = "CUTE_SAKIKO_MOD_CARD_VIOLA_GARBLED.description";
    public const string GarbageVariable = "Garbage";

    private static readonly char[] HanPool =
        "的一是了我不人在他有这个上们来到时大地为子中你说生国年着就那和要她出也得里后自以会家可下而过天去能对小多然于心学么之都好看起发当没成只如事把还用第样道想作种开美总从无情己面最女但现前些所同日手又行意动方期它头经长儿回位分爱老因很给名法间斯知世什两次使身者被高已亲其进此话常与活正感".ToCharArray();

    private static readonly char[] KanaPool =
        "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをんアイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン".ToCharArray();

    private static readonly char[] LatinPool =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

    private static readonly char[] SymbolPool = "▓▒░◇◆▨※§¶".ToCharArray();

    public static bool IsEroded(CardModel card) => card.Affliction is ViolaErosion;

    // 每张卡每个位置各自保留一个字符缓冲：每次刷新只随机替换其中一部分位置，
    // 这样字是"一个个错开变"的，而不是整串一起跳。
    // 注意：ConditionalWeakTable.GetOrCreateValue 会用无参构造造 TValue，char[] 不支持，
    // 必须用 TryGetValue + Add 手动管理（否则运行时报 MissingMethodException）。
    private sealed class CharBuffer
    {
        public char[] Chars = [];
    }

    private static readonly ConditionalWeakTable<CardModel, CharBuffer> TitleBuffers = new();
    private static readonly ConditionalWeakTable<CardModel, CharBuffer> DescriptionBuffers = new();
    private static readonly Random Rng = new();

    private const int TitleLength = 6;
    private const int DescriptionLength = 26;

    public static LocString BuildGarbledDescription(CardModel card)
    {
        var loc = new LocString("cards", GarbledDescriptionKey);
        loc.Add(GarbageVariable, StepBuffer(card, DescriptionBuffers, DescriptionLength));
        return loc;
    }

    public static string BuildGarbledTitle(CardModel card)
    {
        return StepBuffer(card, TitleBuffers, TitleLength);
    }

    private static string StepBuffer(CardModel card, ConditionalWeakTable<CardModel, CharBuffer> table, int length)
    {
        if (!table.TryGetValue(card, out var buffer) || buffer.Chars.Length != length)
        {
            buffer = new CharBuffer { Chars = new char[length] };
            for (var i = 0; i < length; i++) buffer.Chars[i] = RandomChar();

            if (table.TryGetValue(card, out _)) table.Remove(card);
            table.Add(card, buffer);
            return new string(buffer.Chars);
        }

        // 每次只换约 35% 的位置 → 每个字自己的"跳变时刻"不同
        var changed = 0;
        for (var i = 0; i < length; i++)
        {
            if (Rng.NextDouble() > 0.35) continue;
            buffer.Chars[i] = RandomChar();
            changed++;
        }

        if (changed == 0) buffer.Chars[Rng.Next(length)] = RandomChar();
        return new string(buffer.Chars);
    }

    /// <summary>能量数字用：1~2 个字符的短乱码。</summary>
    public static string BuildShortGarbage()
    {
        return Rng.Next(2) == 0 ? RandomChar().ToString() : new string([RandomChar(), RandomChar()]);
    }
    private static char RandomChar()
    {
        var pool = CurrentPool();
        return Rng.Next(5) == 0
            ? SymbolPool[Rng.Next(SymbolPool.Length)]
            : pool[Rng.Next(pool.Length)];
    }

    private static char[] CurrentPool()
    {
        var lang = LocManager.Instance?.Language ?? "eng";
        return lang switch
        {
            "zhs" or "zht" => HanPool,
            "jpn" => KanaPool,
            _ => LatinPool
        };
    }
}

/// <summary>卡图替换：被侵蚀时直接返回 viola 图（先古卡也套这张，视觉上等同普通样式）。</summary>
[HarmonyPatch(typeof(CardModel), "get_PortraitPath")]
public static class CardPortraitErosionPatch
{
    public static void Postfix(CardModel __instance, ref string __result)
    {
        if (ViolaErosionText.IsEroded(__instance))
            __result = ViolaErosionText.CardImagePath;
    }
}

/// <summary>标题替换：注意标题不是富文本，不能写 [color]/[gold] 这类标签。</summary>
[HarmonyPatch(typeof(CardModel), "get_Title")]
public static class CardTitleErosionPatch
{
    public static void Postfix(CardModel __instance, ref string __result)
    {
        if (!ViolaErosionText.IsEroded(__instance)) return;

        __result = ViolaErosionText.BuildGarbledTitle(__instance);
    }
}

/// <summary>描述替换。</summary>
[HarmonyPatch(typeof(CardModel), "get_Description")]
public static class CardDescriptionErosionPatch
{
    public static void Postfix(CardModel __instance, ref LocString __result)
    {
        if (ViolaErosionText.IsEroded(__instance))
            __result = ViolaErosionText.BuildGarbledDescription(__instance);
    }
}

/// <summary>
///     被侵蚀的牌不再显示关键词/能力等悬浮提示——否则玩家看提示框就能猜出这是什么牌。
///     只保留 affliction 自己的提示（"无法辨认"），说明状态但不泄露内容。
/// </summary>
[HarmonyPatch(typeof(CardModel), "get_HoverTips")]
public static class CardHoverTipsErosionPatch
{
    public static void Postfix(CardModel __instance, ref IEnumerable<IHoverTip> __result)
    {
        if (!ViolaErosionText.IsEroded(__instance)) return;

        __result = __instance.Affliction is { } affliction
            ? [affliction.HoverTip]
            : [];
    }
}

/// <summary>
///     卡面视觉：能量数字乱码 + 卡框染成薇欧拉的绿（#384433）。
///     UpdateEnergyCostVisuals / UpdatePortrait 都是 NCard 的私有方法，只能 patch + 反射改私有字段。
/// </summary>
internal static class ViolaErosionVisual
{
    private static readonly Color FrameModulate = new("#384433");

    private static readonly System.Reflection.FieldInfo? EnergyLabelField =
        typeof(NCard).GetField("_energyLabel",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

    private static readonly System.Reflection.FieldInfo? FrameField =
        typeof(NCard).GetField("_frame",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

    public static void ApplyEnergyGarbage(NCard node)
    {
        if (node.Model is not { } card || !ViolaErosionText.IsEroded(card)) return;
        if (EnergyLabelField?.GetValue(node) is not MegaLabel label) return;

        // X / ? 这类特殊显示保留，其余数字换成乱码字符
        var current = label.Text;
        if (string.IsNullOrEmpty(current) || current is "X" or "?") return;

        label.SetTextAutoSize(ViolaErosionText.BuildShortGarbage());
    }

    public static void ApplyFrameTint(NCard node)
    {
        if (node.Model is not { } card) return;
        if (FrameField?.GetValue(node) is not TextureRect frame) return;

        frame.Modulate = ViolaErosionText.IsEroded(card) ? FrameModulate : Colors.White;
    }
}

[HarmonyPatch(typeof(NCard), "UpdateEnergyCostVisuals")]
public static class CardEnergyGarbagePatch
{
    public static void Postfix(NCard __instance) => ViolaErosionVisual.ApplyEnergyGarbage(__instance);
}

[HarmonyPatch(typeof(NCard), "UpdatePortrait")]
public static class CardFrameTintPatch
{
    public static void Postfix(NCard __instance) => ViolaErosionVisual.ApplyFrameTint(__instance);
}
