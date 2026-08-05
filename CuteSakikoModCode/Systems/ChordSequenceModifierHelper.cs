using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public static class ChordSequenceModifierHelper
{
    // ★ 用于存储卡牌直接提供的临时修改器，按玩家 -> 和弦ID -> 修改器 存储
    private static readonly Dictionary<Player, Dictionary<string, ChordSequenceModifier>> _cardModifiers = new();

    /// <summary>
    ///     为某个玩家的特定和弦设置临时修改器（由卡牌直接调用）
    /// </summary>
    public static void SetCardModifier(Player player, string chordId, ChordSequenceModifier modifier)
    {
        if (!_cardModifiers.ContainsKey(player))
            _cardModifiers[player] = new Dictionary<string, ChordSequenceModifier>();

        _cardModifiers[player][chordId] = modifier;
    }

    /// <summary>
    ///     清除某个玩家的所有卡牌临时修改器（可在战斗结束时调用）
    /// </summary>
    public static void ClearCardModifiers(Player player)
    {
        _cardModifiers.Remove(player);
    }

    /// <summary>
    ///     收集生物身上所有活跃的修改器（Power、Relic、卡牌）
    /// </summary>
    public static List<ChordSequenceModifier> CollectModifiers(Creature creature, ChordDefinition chordDef)
    {
        var result = new List<ChordSequenceModifier>();
        if (creature == null) return result;

        // 1. ★ 来自卡牌直接提供的临时修改器（按和弦ID精准匹配）
        var player = creature.Player;
        if (player != null && _cardModifiers.TryGetValue(player, out var dict))
            if (dict.TryGetValue(chordDef.Id, out var mod))
                result.Add(mod);

        // 来自 Power 的修改器
        foreach (var provider in creature.Powers.OfType<IChordSequenceModifierProvider>())
        {
            var cats = provider.AffectedCategories;
            if (cats == null || !cats.Any() || cats.Contains(chordDef.Category))
                result.AddRange(provider.GetModifiers(creature, chordDef));  // 传入 chordDef
        }
        
        // 来自遗物的修改器
        if (player != null)
            foreach (var provider in player.Relics.OfType<IChordSequenceModifierProvider>())
            {
                var cats = provider.AffectedCategories;
                if (cats == null || !cats.Any() || cats.Contains(chordDef.Category))
                    result.AddRange(provider.GetModifiers(creature, chordDef));  // 传入 chordDef
            }
        return result;
    }

    /// <summary>
    ///     依次应用所有修改器，获得修改后的音符序列
    /// </summary>
    public static IReadOnlyList<CardType> GetModifiedSequence(ChordDefinition chordDef, Creature owner)
    {
        var mods = CollectModifiers(owner, chordDef);
        IReadOnlyList<CardType> seq = chordDef.NoteSequence;
        foreach (var mod in mods)
            seq = mod.Apply(seq);
        return seq;
    }
    
    /// <summary>
    /// 移除某个玩家特定和弦的卡牌临时修改器。
    /// </summary>
    public static void RemoveCardModifier(Player player, string chordId)
    {
        if (_cardModifiers.TryGetValue(player, out var dict))
        {
            dict.Remove(chordId);
            if (dict.Count == 0)
                _cardModifiers.Remove(player);
        }
    }

    /// <summary>
    ///     生成修改后的条件文本（用于 UI）
    /// </summary>
    public static string GetModifiedConditionText(ChordDefinition chordDef, Creature owner)
    {
        var seq = GetModifiedSequence(chordDef, owner);
        var parts = new List<string>();
        foreach (var t in seq)
        {
            string text;
            string color;

            if (t == Entry.AnyNote)
            {
                text = new LocString("static_hover_tips", "CUTE_SAKIKO_MOD_CONDITION_ANY").GetFormattedText();
                color = "gray";
            }
            else switch (t)
            {
                case CardType.Attack:
                    text = new LocString("static_hover_tips", "CUTE_SAKIKO_MOD_CONDITION_ATTACK").GetFormattedText();
                    color = "red";
                    break;
                case CardType.Skill:
                    text = new LocString("static_hover_tips", "CUTE_SAKIKO_MOD_CONDITION_SKILL").GetFormattedText();
                    color = "blue";
                    break;
                case CardType.Power:
                    text = new LocString("static_hover_tips", "CUTE_SAKIKO_MOD_CONDITION_POWER").GetFormattedText();
                    color = "gold";
                    break;
                default:
                    text = new LocString("static_hover_tips", "CUTE_SAKIKO_MOD_CONDITION_STATUS").GetFormattedText();
                    color = "pink";
                    break;
            }

            parts.Add($"[{color}]{text}[/{color}]");
        }
        return string.Join(" ", parts);
    }
}