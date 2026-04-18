using Godot;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.HoverTips; // 新增引用
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace CuteSakikoMod.CuteSakikoModCode.Systems
{
    public static class ChordDisplayHelper
    {
        private static readonly Dictionary<string, (string TitleKey, string DescKey, string Condition, string IconPath)> ChordData = new()
        {
            ["C"] = ("CUTESAKIKOMOD-CCHORD.title", "CUTESAKIKOMOD-CCHORD.description", "[red]攻[/red] [red]攻[/red] [red]攻[/red]", "c_chord.png"),
            ["G"] = ("CUTESAKIKOMOD-GCHORD.title", "CUTESAKIKOMOD-GCHORD.description", "[red]攻[/red] [red]攻[/red] [red]攻[/red] [red]攻[/red]", "g_chord.png"),
            ["Am"] = ("CUTESAKIKOMOD-AMCHORD.title", "CUTESAKIKOMOD-AMCHORD.description", "[blue]技[/blue] [blue]技[/blue] [blue]技[/blue] [blue]技[/blue]", "am_chord.png"),
            ["F"] = ("CUTESAKIKOMOD-FCHORD.title", "CUTESAKIKOMOD-FCHORD.description", "[gold]能[/gold] [blue]技[/blue] [red]攻[/red]", "f_chord.png")
        };

        public static Texture2D GetChordTexture(string chord)
        {
            if (ChordManager.AllChords.TryGetValue(chord, out var def))
            {
                // 假设图标存放在 Mod 的 images/chords/ 目录下
                string path = $"CuteSakikoMod/images/ui/chords/{def.IconName}.png";
                return GD.Load<Texture2D>(path);
            }
            return null;
        }
        public static string GetFormattedDescription(ChordDefinition def, int multiplier)
        {
            var rawDesc = new LocString("card_keywords", def.DescKey).GetRawText();
            if (def.BaseValues == null || def.BaseValues.Length == 0)
                return rawDesc;
            var values = def.BaseValues.Select(v => (v * multiplier).ToString()).ToArray();
            try
            {
                return string.Format(rawDesc, values);
            }
            catch
            {
                return rawDesc;
            }
        }

        public static string GetChordTooltip(string chord)
        {
            if (!ChordData.TryGetValue(chord, out var data))
                return "未知和弦";
            string title = new LocString("card_keywords", data.TitleKey).GetFormattedText();
            string description = new LocString("card_keywords", data.DescKey).GetFormattedText();
            return $"{title}\n{description}";
        }

        public static List<string> GetChordInfoStrings(IReadOnlyList<string> chords)
        {
            var list = new List<string>();
            foreach (var chord in chords)
            {
                if (!ChordData.TryGetValue(chord, out var data))
                    continue;
                var title = new LocString("card_keywords", data.TitleKey);
                var desc = new LocString("card_keywords", data.DescKey);
                list.Add($"[{title.GetFormattedText()}]({data.Condition})\n{desc.GetFormattedText() ?? "无效果"}");
            }
            return list;
        }

        // 新增方法：返回可用于悬停提示的 HoverTip 对象
        public static HoverTip GetChordHoverTip(string chord, int multiplier = 1)
        {
            if (ChordManager.AllChords.TryGetValue(chord, out var def))
            {
                var title = new LocString("card_keywords", def.TitleKey);
                string descText = GetFormattedDescription(def, multiplier);
                return new HoverTip(title, descText);
            }
            return new HoverTip(new LocString("card_keywords", "CUTESAKIKOMOD-CCHORD.title"), "未知和弦");
        }
        public static HoverTip GetNoteTypeHoverTip(CardType type)
        {
            string key;
            switch (type)
            {
                case CardType.Attack:
                    key = "CUTESAKIKOMOD_NOTE_ATTACK";
                    break;
                case CardType.Skill:
                    key = "CUTESAKIKOMOD_NOTE_SKILL";
                    break;
                case CardType.Power:
                    key = "CUTESAKIKOMOD_NOTE_POWER";
                    break;
                default:
                    key = "CUTESAKIKOMOD_NOTE_SPECIAL";
                    break;
            }
            var title = new LocString("static_hover_tips", $"{key}.title");
            var desc = new LocString("static_hover_tips", $"{key}.description");
            return new HoverTip(title, desc);
        }
    }
    
}