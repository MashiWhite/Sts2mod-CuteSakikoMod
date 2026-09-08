using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Memory
{
    /// <summary>
    /// 实现此接口的模型将参与遗忘事件的 BeforeForget / AfterForget 钩子。
    /// </summary>
    public interface IForgetHookHandler
    {
        /// <summary>
        /// 在卡牌被移入遗忘堆之前触发。应只包含不改变牌堆结构的确定性效果（如伤害）。
        /// </summary>
        Task BeforeForget(PlayerChoiceContext choiceContext, IReadOnlyList<CardModel> cards, CardModel? source);

        /// <summary>
        /// 在卡牌被移入遗忘堆之后触发。可以包含改变牌堆结构的效果（如抽牌）。
        /// </summary>
        Task AfterForget(PlayerChoiceContext choiceContext, IReadOnlyList<CardModel> cards, CardModel? source);
    }
}