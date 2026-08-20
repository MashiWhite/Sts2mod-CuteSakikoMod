using System.Diagnostics;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Chord;

public static class ChordSequenceModifierHelper
{
    private static readonly Dictionary<Player, Dictionary<string, ChordSequenceModifier>> _cardModifiers = new();
    private static int _getModifiedSequenceCallCount = 0; // 全局调用计数器

    public static void SetCardModifier(Player player, string chordId, ChordSequenceModifier modifier)
    {
        if (!_cardModifiers.ContainsKey(player))
            _cardModifiers[player] = new Dictionary<string, ChordSequenceModifier>();

        _cardModifiers[player][chordId] = modifier;
    }

    public static void ClearCardModifiers(Player player)
    {
        _cardModifiers.Remove(player);
    }

    public static List<ChordSequenceModifier> CollectModifiers(Creature creature, ChordDefinition chordDef)
    {
        var result = new List<ChordSequenceModifier>();
        if (creature == null) return result;

        // 记录调用上下文
        var stackTrace = new StackTrace(true);
        var caller = stackTrace.GetFrame(1)?.GetMethod()?.Name ?? "Unknown";
        var log = $"[CollectModifiers] Called for chord {chordDef.Id} from {caller}";
        Entry.Logger.Debug(log);

        var player = creature.Player;
        if (player != null && _cardModifiers.TryGetValue(player, out var dict))
            if (dict.TryGetValue(chordDef.Id, out var mod))
            {
                result.Add(mod);
                Entry.Logger.Debug($"[CollectModifiers] Added card modifier for {chordDef.Id}");
            }

        // 来自 Power 的修改器
        foreach (var provider in creature.Powers.OfType<IChordSequenceModifierProvider>())
        {
            var cats = provider.AffectedCategories;
            if (cats == null || !cats.Any() || cats.Contains(chordDef.Category))
            {
                var mods = provider.GetModifiers(creature, chordDef).ToList();
                if (mods.Count > 0)
                    Entry.Logger.Debug($"[CollectModifiers] Power {provider.GetType().Name} added {mods.Count} mod(s) for {chordDef.Id}");
                result.AddRange(mods);
            }
        }
        
        if (player != null)
            foreach (var provider in player.Relics.OfType<IChordSequenceModifierProvider>())
            {
                var cats = provider.AffectedCategories;
                if (cats == null || !cats.Any() || cats.Contains(chordDef.Category))
                {
                    var mods = provider.GetModifiers(creature, chordDef).ToList();
                    if (mods.Count > 0)
                        Entry.Logger.Debug($"[CollectModifiers] Relic {provider.GetType().Name} added {mods.Count} mod(s) for {chordDef.Id}");
                    result.AddRange(mods);
                }
            }
        return result;
    }

    public static IReadOnlyList<CardType> GetModifiedSequence(ChordDefinition chordDef, Creature owner)
    {
        _getModifiedSequenceCallCount++;
        var stackTrace = new StackTrace(true);
        var caller = stackTrace.GetFrame(1)?.GetMethod()?.Name ?? "Unknown";
        var log = $"[GetModifiedSequence] #{_getModifiedSequenceCallCount} for chord {chordDef.Id} from {caller}, owner={owner?.Player?.NetId}";
        Entry.Logger.Debug(log);

        var mods = CollectModifiers(owner, chordDef);
        IReadOnlyList<CardType> seq = chordDef.NoteSequence;
        foreach (var mod in mods)
            seq = mod.Apply(seq);
        return seq;
    }
    
    public static void RemoveCardModifier(Player player, string chordId)
    {
        if (_cardModifiers.TryGetValue(player, out var dict))
        {
            dict.Remove(chordId);
            if (dict.Count == 0)
                _cardModifiers.Remove(player);
        }
    }

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
                color = "pink";
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
                    color = "purple";
                    break;
            }

            parts.Add($"[{color}]{text}[/{color}]");
        }
        return string.Join(" ", parts);
    }
}