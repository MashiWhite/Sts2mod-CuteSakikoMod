using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Chord;

public enum ChordCategory
{
    Major, Minor, Dominant, Bonus, Anon
}

public class ChordDefinition
{
    public string Id { get; set; }
    public ChordCategory Category { get; set; }
    public CardType[] NoteSequence { get; set; }
    public int[] BaseValues { get; set; }
    public string TitleKey { get; set; }
    public string DescKey { get; set; }
    public string IconName { get; set; }
    public bool IsTemporaryOnly { get; set; }

    // 新版委托：bonus 为加算数值
    public Func<PlayerChoiceContext, Creature, int, Task> Effect { get; set; }
    
    public string GetConditionText()
    {
        var parts = new List<string>();
        foreach (var type in NoteSequence)
        {
            string text;

            if (type == Entry.AnyNote)
            {
                text = new LocString("static_hover_tips", "CUTE_SAKIKO_MOD_CONDITION_ANY").GetFormattedText();
            }
            else switch (type)
            {
                case CardType.Attack:
                    text = new LocString("static_hover_tips", "CUTE_SAKIKO_MOD_CONDITION_ATTACK").GetFormattedText();
                    break;
                case CardType.Skill:
                    text = new LocString("static_hover_tips", "CUTE_SAKIKO_MOD_CONDITION_SKILL").GetFormattedText();
                    break;
                case CardType.Power:
                    text = new LocString("static_hover_tips", "CUTE_SAKIKO_MOD_CONDITION_POWER").GetFormattedText();
                    break;
                default:
                    text = new LocString("static_hover_tips", "CUTE_SAKIKO_MOD_CONDITION_STATUS").GetFormattedText();
                    break;
            }

            parts.Add($"{text}");
        }
        return string.Join(" ", parts);
    }
}