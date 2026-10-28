using System.Runtime.CompilerServices;
using CuteSakikoMod.CuteSakikoModCode.Others.Afflictions;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
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
    private const int TitleLength = 6;
    private static readonly Random Rng = new();

    // 每个字独立的"跳变节奏"：位置 i 有自己的相位偏移和周期，
    // 这样整串不会看起来一起变，而是此起彼伏地错开跳。
    private static readonly double[] SlotPhase = BuildSlots(seed: 0.37, count: 40);
    private static readonly double[] SlotRate = BuildSlots(seed: 0.11, count: 40);

    private static double[] BuildSlots(double seed, int count)
    {
        var slots = new double[count];
        var rng = new Random(1717);
        for (var i = 0; i < count; i++)
            slots[i] = seed * i + rng.NextDouble() * 0.9;
        return slots;
    }

    private static readonly char[] HanPool =
        "薇欧拉我好很特别喜欢离不开爱你想念恋情心愿陪伴梦漫画粉丝理解蛇谎言假真意迷惑忘记忆思绪望着等永远".ToCharArray();

    private static readonly char[] KanaPool =
        "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをんアイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン".ToCharArray();

    private static readonly char[] LatinPool =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();

    public static bool IsEroded(CardModel card) => card.Affliction is ViolaErosion;


    public static LocString BuildGarbledDescription(CardModel card)
    {
        // 不用 [viola]/[scramble] 字形替换：RichTextEffect 的 SetGlyphIndex 只换字形、不重新排版，
        // 换进去的字符 advance 宽度和原字不同就会挤压重叠（实测描述里会出现叠字）。
        // 所以描述也走"纯文本乱码 + ticker 定时换字"，排版完全正常。
        var loc = new LocString("cards", GarbledDescriptionKey);
        loc.Add(GarbageVariable, BuildDescriptionText());
        return loc;
    }

    /// <summary>
    ///     描述用的乱码文本。
    ///     中文/日文用 [viola] 包起来——由 RichTextLabel 渲染阶段逐字换字形，不需要 ticker，开销接近 0；
    ///     且中日文全角等宽，换字形不会位移。英文是比例字体，换字形会挤，所以英文仍走纯文本（由 ticker 刷新）。
    /// </summary>
    public static string BuildDescriptionText()
    {
        // 方案 A：纯文本乱码 + ticker 换字。
        // 之前用 [viola] 做字形替换（引擎驱动、零开销），但 SetGlyphIndex 只换字形不重排版，
        // 且注入效果的时机在 ParseBbcode 之后，标签会被当文本渲染、宽度也乱 → 叠字。
        // 所以描述统一走纯文本，由 ticker 定时换字，排版绝对正确。
        // 三行按"行号"分开：行种子由固定基数 + 行号组成，
        // 同一张卡的三行内容各不相同，且不会被每次刷新打乱成随机。
        return string.Join("\n",
            BuildLine(11, LineSeed(0)),
            BuildLine(11, LineSeed(1)),
            BuildLine(7, LineSeed(2)));
    }

    /// <summary>中日文才用字形替换（等宽全角）；其他语言退回纯文本。</summary>
    public static bool UsesGlyphScramble()
    {
        var lang = LocManager.Instance?.Language ?? "eng";
        return lang is "zhs" or "zht" or "jpn";
    }

    public static string BuildGarbledTitle(CardModel card) => RandomGarbage(TitleLength);


    /// <summary>能量数字用：1~2 个字符的短乱码（可能是 emoji，所以用 string 拼）。</summary>
    public static string BuildShortGarbage()
    {
        return Rng.Next(2) == 0 ? RandomToken() : RandomToken() + RandomToken();
    }

    /// <summary>
    ///     生成固定长度乱码文本。
    ///     每个位置按自己的节奏（SlotPhase/SlotRate）决定这一步要不要换字，
    ///     所以整串是"此起彼伏"地变，而不是一起刷新。
    /// </summary>
    public static string RandomGarbage(int length)
    {
        // 每一行用一个独立种子，避免"三行完全一样"
        return BuildLine(length, lineSeed: Rng.Next());
    }

    /// <summary>行种子：同一行固定，不同行不同。</summary>
    private static int LineSeed(int line) => 1000003 + line * 7919;
    /// <summary>生成一行乱码（多行时每行传不同 lineSeed）。</summary>
    public static string BuildLine(int length, int lineSeed)
    {
        var builder = new System.Text.StringBuilder(length * 2);
        var now = NowSeconds();

        for (var i = 0; i < length; i++)
        {
            var slot = i % SlotPhase.Length;
            // 每个位置节奏不同（约 3.5~7 次/秒），既有停留感又不会显得卡住
            var rate = 3.5 + SlotRate[slot] * 3.5;
            var phase = now * rate + SlotPhase[slot] + lineSeed * 0.017;

            var step = (int)Math.Floor(phase);
            builder.Append(PoolChar(slot, step, lineSeed));
        }

        return builder.ToString();
    }

    /// <summary>按（位置, 步数, 行种子）取稳定字符：同一步不变，跨步才变。</summary>
    private static char PoolChar(int slot, int step, int lineSeed)
    {
        var pool = CurrentPool();
        unchecked
        {
            var h = step * 73856093 ^ slot * 19349663 ^ lineSeed * 83492791;
            h ^= h >> 13;
            h *= 1274126177;
            var idx = Math.Abs(h ^ (h >> 16)) % pool.Length;
            return pool[idx];
        }
    }

    private static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();

    private static double NowSeconds() => Clock.Elapsed.TotalSeconds;

    private static string RandomToken()
    {
        // 只用当前语言的单字字符池（不含符号/emoji），保证卡面字体都有字形、宽度一致
        var pool = CurrentPool();
        return pool[Rng.Next(pool.Length)].ToString();
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
///     先古卡走的是另一套节点（_ancientPortrait / _ancientBorder / _ancientTextBg），要单独处理。
/// </summary>
internal static class ViolaErosionVisual
{
    private static readonly Color FrameModulate = new("#384433");

    private static System.Reflection.FieldInfo? Field(string name) =>
        typeof(NCard).GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

    private static readonly System.Reflection.FieldInfo? EnergyLabelField = Field("_energyLabel");
    private static readonly System.Reflection.FieldInfo? TitleLabelField = Field("_titleLabel");
    private static readonly System.Reflection.FieldInfo? DescriptionLabelField = Field("_descriptionLabel");
    private static readonly System.Reflection.FieldInfo? TypeLabelField = Field("_typeLabel");
    private static readonly System.Reflection.FieldInfo? StarLabelField = Field("_starLabel");
    private static readonly System.Reflection.FieldInfo? FrameField = Field("_frame");
    private static readonly System.Reflection.FieldInfo? PortraitField = Field("_portrait");
    private static readonly System.Reflection.FieldInfo? PortraitBorderField = Field("_portraitBorder");
    private static readonly System.Reflection.FieldInfo? BannerField = Field("_banner");
    private static readonly System.Reflection.FieldInfo? AncientPortraitField = Field("_ancientPortrait");
    private static readonly System.Reflection.FieldInfo? AncientBorderField = Field("_ancientBorder");
    private static readonly System.Reflection.FieldInfo? AncientTextBgField = Field("_ancientTextBg");
    private static readonly System.Reflection.FieldInfo? AncientBannerField = Field("_ancientBanner");
    private static readonly System.Reflection.FieldInfo? AncientGlassField = Field("_ancientBorderGlassOverlay");

    /// <summary>
    ///     被侵蚀时的完整卡面：贴 viola 图、隐藏先古节点、显示普通节点、写入诅咒材质。
    ///     与 ApplyFrameTint 的区别是它同时负责纹理（因为跳过原 UpdatePortrait 后纹理不会再被设置）。
    /// </summary>
    public static void ApplyErodedPortrait(NCard node)
    {
        var texture = GD.Load<Texture2D>(ViolaErosionText.CardImagePath);
        var curseFrame = CurseFrameMaterial;
        var curseBanner = CurseBannerMaterial;

        // 先古节点全部隐藏
        if (AncientPortraitField?.GetValue(node) is TextureRect ancientPortrait) ancientPortrait.Visible = false;
        if (AncientBorderField?.GetValue(node) is TextureRect ancientBorder) ancientBorder.Visible = false;
        if (AncientTextBgField?.GetValue(node) is TextureRect ancientTextBg) ancientTextBg.Visible = false;
        if (AncientGlassField?.GetValue(node) is TextureRect ancientGlass) ancientGlass.Visible = false;
        if (AncientBannerField?.GetValue(node) is Control ancientBanner) ancientBanner.Visible = false;

        // 普通卡节点：显示 + viola 图 + 诅咒材质
        if (PortraitField?.GetValue(node) is TextureRect portrait)
        {
            portrait.Visible = true;
            portrait.Texture = texture;
        }

        if (PortraitBorderField?.GetValue(node) is TextureRect border)
        {
            border.Visible = true;
            border.Modulate = Colors.White;
            if (curseBanner != null) border.Material = curseBanner;
        }

        if (FrameField?.GetValue(node) is TextureRect frame)
        {
            frame.Visible = true;
            frame.Modulate = Colors.White;
            if (curseFrame != null) frame.Material = curseFrame;
        }

        if (BannerField?.GetValue(node) is TextureRect banner)
        {
            banner.Visible = true;
            if (curseBanner != null) banner.Material = curseBanner;
        }
    }
    /// <summary>把全部侵蚀视觉一次性贴好（卡图/卡框/稀有度/标题/能量星辉/描述）。</summary>
    public static void ApplyAll(NCard node)
    {
        if (node.Model is not { } card || !ViolaErosionText.IsEroded(card)) return;

        ApplyFrameTint(node);
        ApplyCurseRarity(node);
        ApplyTitle(node);
        ApplyEnergyGarbage(node);

        if (DescriptionLabelField?.GetValue(node) is MegaRichTextLabel description)
            description.SetTextAutoSize("[center]" + ViolaErosionText.BuildDescriptionText() + "[/center]");
    }
    public static void ApplyEnergyGarbage(NCard node)
    {
        if (node.Model is not { } card || !ViolaErosionText.IsEroded(card)) return;

        // 能量数字
        if (EnergyLabelField?.GetValue(node) is MegaLabel label)
            GarbageNumber(label);

        // 星辉数字（同样要乱码，否则能看出这张牌花多少星）
        if (StarLabelField?.GetValue(node) is MegaLabel starLabel)
            GarbageNumber(starLabel);
    }

    /// <summary>把纯数字的 Label 换成短乱码；X / ? / 空 保留原样。</summary>
    private static void GarbageNumber(MegaLabel label)
    {
        var current = label.Text;
        if (string.IsNullOrEmpty(current) || current is "X" or "?") return;

        label.SetTextAutoSize(ViolaErosionText.BuildShortGarbage());
    }

    /// <summary>
    ///     标题：MegaLabel 不支持 BBCode，只能用纯文本乱码，由 FraudulentSerpentPower 的 ticker 定时换字。
    ///     之前试过在标题上叠 RichTextLabel + [viola] 效果，但会出现字形缺失导致字宽为 0、文字重叠，
    ///     所以标题保持旧方案（静态乱码 + ticker 刷新）。
    /// </summary>
    public static void ApplyTitle(NCard node)
    {
        if (node.Model is not { } card) return;
        if (TitleLabelField?.GetValue(node) is not MegaLabel titleLabel) return;

        if (!ViolaErosionText.IsEroded(card)) return;

        titleLabel.SetTextAutoSize(ViolaErosionText.BuildGarbledTitle(card));
    }

    /// <summary>
    ///     轻量刷新：只换标题、描述、能量数字三个文本，不碰贴图/布局/动态变量。
    ///     NCard.UpdateVisuals 会重载纹理、重算 DynamicVars、重新解析描述，每秒 10 次 × N 张卡会掉帧。
    /// </summary>
    public static void RefreshTextsOnly(NCard node)
    {
        if (node.Model is not { } card || !ViolaErosionText.IsEroded(card)) return;

        ApplyTitle(node);
        ApplyEnergyGarbage(node);
        ApplyCurseTitleColor(node);
        ApplyCurseRarity(node);

        if (DescriptionLabelField?.GetValue(node) is MegaRichTextLabel description)
        {
            description.SetTextAutoSize(
                "[center]" + ViolaErosionText.BuildDescriptionText() + "[/center]");
        }
    }


    /// <summary>
    ///     稀有度伪装成"诅咒"（紫卡）：游戏里稀有度不体现在 Frame 纹理上（Frame 按 CardType 取），
    ///     而是体现在卡框材质（card_frame_curse_mat）和标题横幅材质（card_banner_curse_mat）。
    ///     这里把 _frame 的材质换成诅咒版，并把类型标签改成"诅咒"。
    /// </summary>
    public static void ApplyCurseRarity(NCard node)
    {
        if (node.Model is not { } card || !ViolaErosionText.IsEroded(card)) return;

        var curseFrame = CurseFrameMaterial;
        var curseBanner = CurseBannerMaterial;

        if (FrameField?.GetValue(node) is TextureRect frame && curseFrame != null)
            frame.Material = curseFrame;

        if (BannerField?.GetValue(node) is TextureRect banner && curseBanner != null)
            banner.Material = curseBanner;

        if (TypeLabelField?.GetValue(node) is MegaLabel typeLabel)
            typeLabel.SetTextAutoSize(CurseTypeText);
    }

    public static Material? CurseFrameMaterial { get; } =
        GD.Load<Material>("res://materials/cards/frames/card_frame_curse_mat.tres");

    public static Material? CurseBannerMaterial { get; } =
        GD.Load<Material>("res://materials/cards/banners/card_banner_curse_mat.tres");
    private static readonly string CurseTypeText =
        new LocString("gameplay_ui", "CARD_TYPE.CURSE").GetFormattedText();

    /// <summary>
    ///     稀有度显示伪装成"诅咒"：卡面没有独立的稀有度节点，稀有度体现在标题描边色上，
    ///     所以把标题色彩强制为诅咒色（UpdateTitleLabel 会按 Rarity 重设，这里每次都要盖一次）。
    /// </summary>
    public static void ApplyCurseTitleColor(NCard node)
    {
        if (node.Model is not { } card || !ViolaErosionText.IsEroded(card)) return;
        if (TitleLabelField?.GetValue(node) is not MegaLabel titleLabel) return;

        titleLabel.AddThemeColorOverride(ThemeConstants.Label.FontColor, StsColors.cream);
        titleLabel.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor,
            StsColors.cardTitleOutlineCurse);
    }

    /// <summary>
    ///     卡图与卡框：被侵蚀时伪装成"普通卡样式"。
    ///     做法逆向自 RitsuLib 的 CardVisualOverrides.ApplyStyle——它靠切换节点显隐实现先古↔普通，
    ///     而不是改 Rarity。这里照做：隐藏先古节点、显示普通节点、贴 viola 图并染绿。
    /// </summary>
    public static void ApplyFrameTint(NCard node)
    {
        if (node.Model is not { } card)
            return;

        // 只有被侵蚀时才动视觉；未侵蚀必须完全不插手，否则会把普通卡/先古卡的节点
        // 显隐改成原版之外的组合（之前"命运的相遇"不显示先古样式就是这个 bug）。
        if (!ViolaErosionText.IsEroded(card)) return;

        var texture = GD.Load<Texture2D>(ViolaErosionText.CardImagePath);
        var curseBanner = CurseBannerMaterial;
        var curseFrame = CurseFrameMaterial;

        // 隐藏先古节点
        if (AncientPortraitField?.GetValue(node) is TextureRect ancientPortrait)
            ancientPortrait.Visible = false;

        if (AncientBorderField?.GetValue(node) is TextureRect ancientBorder)
            ancientBorder.Visible = false;

        if (AncientTextBgField?.GetValue(node) is TextureRect ancientTextBg)
            ancientTextBg.Visible = false;

        if (AncientGlassField?.GetValue(node) is TextureRect ancientGlass)
            ancientGlass.Visible = false;

        if (AncientBannerField?.GetValue(node) is Control ancientBanner)
            ancientBanner.Visible = false;

        // 强制显示普通节点，形成"普通卡外观"
        if (PortraitField?.GetValue(node) is TextureRect portrait)
        {
            portrait.Visible = true;
            if (texture != null) portrait.Texture = texture;
        }

        if (PortraitBorderField?.GetValue(node) is TextureRect border)
        {
            border.Visible = true;
            border.Modulate = Colors.White;
            if (curseBanner != null) border.Material = curseBanner;
        }

        if (FrameField?.GetValue(node) is TextureRect frame)
        {
            frame.Visible = true;
            frame.Modulate = Colors.White;
            if (curseFrame != null) frame.Material = curseFrame;
        }

        if (BannerField?.GetValue(node) is TextureRect banner)
            banner.Visible = true;
    }
}

[HarmonyPatch(typeof(NCard), "UpdateEnergyCostVisuals")]
public static class CardEnergyGarbagePatch
{
    public static void Postfix(NCard __instance) => ViolaErosionVisual.ApplyEnergyGarbage(__instance);
}

/// <summary>
///     零破坏性方案：不跳过任何原方法，只修改它们拿到的材质。
///     CardModel.BannerMaterial 是按稀有度决定的（CardModel.cs:273-285，Rare → card_banner_rare_mat 就是金色），
///     在这里换成诅咒材质，NCard 原逻辑照常执行但拿到的已经是紫卡色，因此不存在"先写金色再改回"的闪烁。
/// </summary>
[HarmonyPatch(typeof(CardModel), "get_BannerMaterial")]
public static class CardBannerMaterialErosionPatch
{
    public static void Postfix(CardModel __instance, ref Material __result)
    {
        if (!ViolaErosionText.IsEroded(__instance)) return;
        if (ViolaErosionVisual.CurseBannerMaterial is { } curse) __result = curse;
    }
}

/// <summary>
///     卡图纹理与先古/普通节点显隐：只能用 Postfix 追加（不跳过原方法，兼容其他 mod）。
/// </summary>
[HarmonyPatch(typeof(NCard), "UpdatePortrait")]
public static class CardFrameTintPatch
{
    public static void Postfix(NCard __instance)
    {
        if (__instance.Model is not { } card || !ViolaErosionText.IsEroded(card)) return;

        ViolaErosionVisual.ApplyErodedPortrait(__instance);
    }
}

/// <summary>
///     侵蚀视觉必须在所有"重置型"方法之后应用，否则会被覆盖。
///     NCard 里会重置视觉的有：
///       · Reload()          —— UpdateTypePlaque() + 所有节点 Visible（NCard.cs:726）
///       · UpdateVisuals()   —— UpdatePortrait/Title/Energy/Star/TypePlaque 全套
///     所以统一在这两个方法的 postfix 里补一遍（postfix 在方法体执行完之后跑）。
/// </summary>
[HarmonyPatch(typeof(NCard), "Reload")]
public static class CardErosionVisualOnReloadPatch
{
    public static void Postfix(NCard __instance) => ViolaErosionVisual.ApplyAll(__instance);
}

[HarmonyPatch(typeof(NCard), nameof(NCard.UpdateVisuals))]
public static class CardErosionVisualOnUpdateVisualsPatch
{
    public static void Postfix(NCard __instance) => ViolaErosionVisual.ApplyAll(__instance);
}