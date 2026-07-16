using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Systems;

public enum ChordCategory
{
    Major,
    Minor,
    Dominant,
    Bonus,
    Anon
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
    public Func<PlayerChoiceContext, Creature, int, Task> Effect { get; set; }

    public string GetConditionText()
    {
        var parts = new List<string>();
        foreach (var type in NoteSequence)
            parts.Add(type switch
            {
                _ when type == Entry.AnyNote => "[pink]音[/pink]",
                CardType.Attack => "[red]攻[/red]",
                CardType.Skill => "[blue]技[/blue]",
                CardType.Power => "[gold]能[/gold]",
                _ => "特"
            });
        return string.Join(" ", parts);
    }
}