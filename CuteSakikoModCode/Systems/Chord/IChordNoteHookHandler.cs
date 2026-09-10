using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using System.Threading.Tasks;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Chord
{
    /// <summary>
    /// 实现此接口的模型可以接收音符/和弦事件的 Before/After 钩子。
    /// </summary>
    public interface IChordNoteHookHandler
    {
        Task BeforeNoteAdded(Player player, CardType noteType, PlayerChoiceContext? context) => Task.CompletedTask;
        Task AfterNoteAdded(Player player, CardType noteType, PlayerChoiceContext? context) => Task.CompletedTask;
        Task BeforeChordMatched(Player player, string chordId, PlayerChoiceContext? context) => Task.CompletedTask;
        Task AfterChordMatched(Player player, string chordId, PlayerChoiceContext? context) => Task.CompletedTask;
        Task BeforeChordPlayed(Player player, string chordId, int bonus, PlayerChoiceContext? context) => Task.CompletedTask;
        Task AfterChordPlayed(Player player, string chordId, int bonus, PlayerChoiceContext? context) => Task.CompletedTask;
    }
}