using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Chord;

public static class ChordDisplayHelper
{

    public static Texture2D GetChordTexture(string chord)
    {
        if (ChordManager.AllChords.TryGetValue(chord, out var def))
        {
            var path = $"res://CuteSakikoMod/images/ui/chords/{def.IconName}.png";
            return GD.Load<Texture2D>(path);
        }
        return null;
    }

    // ChordDisplayHelper.cs
    public static string GetFormattedDescription(ChordDefinition def, int bonus)
    {
        var rawDesc = new LocString("card_keywords", def.DescKey).GetRawText();
        if (def.BaseValues == null || def.BaseValues.Length == 0)
            return rawDesc;
        var values = def.BaseValues.Select(v => (v + bonus).ToString()).ToArray();
        try
        {
            return string.Format(rawDesc, values);
        }
        catch
        {
            return rawDesc;
        }
    }

    public static HoverTip GetChordHoverTip(string chord, int multiplier = 1)
    {
        if (ChordManager.AllChords.TryGetValue(chord, out var def))
        {
            var title = new LocString("card_keywords", def.TitleKey);
            var descText = GetFormattedDescription(def, multiplier);
            return new HoverTip(title, descText);
        }
        return new HoverTip(new LocString("card_keywords", "CUTESAKIKOMOD-CCHORD.title"), "未知和弦");
    }

    public static HoverTip GetNoteTypeHoverTip(CardType type)
    {
        string key;
        if (type == Entry.AnyNote)
        {
            key = "CUTESAKIKOMOD_NOTE_ANY";
        }
        else switch (type)
        {
            case CardType.Attack: key = "CUTESAKIKOMOD_NOTE_ATTACK"; break;
            case CardType.Skill: key = "CUTESAKIKOMOD_NOTE_SKILL"; break;
            case CardType.Power: key = "CUTESAKIKOMOD_NOTE_POWER"; break;
            default: key = "CUTESAKIKOMOD_NOTE_SPECIAL"; break;
        }
        var title = new LocString("static_hover_tips", $"{key}.title");
        var desc = new LocString("static_hover_tips", $"{key}.description");
        return new HoverTip(title, desc);
    }

    public static HoverTip GetDynamicChordHoverTip(string chordId, Creature owner, int multiplier = 1)
    {
        if (ChordManager.AllChords.TryGetValue(chordId, out var def))
        {
            var title = new LocString("card_keywords", def.TitleKey);
            var effectDesc = GetFormattedDescription(def, multiplier);
            var condition = ChordSequenceModifierHelper.GetModifiedConditionText(def, owner);

            var fullDesc = new LocString("static_hover_tips", "CUTE_SAKIKO_MOD_CHORD_DESC_WITH_CONDITION.description");
            fullDesc.Add("effect", effectDesc);
            fullDesc.Add("condition", condition);

            return new HoverTip(title, fullDesc);
        }
        return new HoverTip(new LocString("card_keywords", "UNKNOWN"), "未知和弦");
    }
}