using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Systems
{
    public enum ChordCategory
    {
        Major,//大三
        Minor,//小三
        Dominant,//属七
        Bonus,   // 新增，用于额外槽位
        Anon //爱音和弦
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
        
        public bool IsTemporaryOnly { get; set; } // 新增，默认为 false
        // 效果执行委托，amountMultiplier 用于先古翻倍
        public Func<PlayerChoiceContext, Creature, int, Task> Effect { get; set; }

        public string GetConditionText()
        {
            var parts = new List<string>();
            foreach (var type in NoteSequence)
            {
                parts.Add(type switch
                {
                    CardType.Attack => "[red]攻[/red]",
                    CardType.Skill => "[blue]技[/blue]",
                    CardType.Power => "[gold]能[/gold]",
                    _ => "[pink]特[/pink]"
                });
            }
            return string.Join(" ", parts);
        }
    }
}