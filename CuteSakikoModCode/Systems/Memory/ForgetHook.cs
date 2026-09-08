
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Systems.Memory
{
    /// <summary>
    /// 遗忘操作的官方风格钩子调度器。
    /// </summary>
    public static class ForgetHook
    {
        /// <summary>
        /// 在遗忘卡牌移动之前调用所有实现了 IForgetHookHandler 的监听者。
        /// </summary>
        public static async Task BeforeForget(
            ICombatState combatState,
            PlayerChoiceContext choiceContext,
            IReadOnlyList<CardModel> cards,
            CardModel? source)
        {
            if (combatState == null || cards.Count == 0) return;

            foreach (var model in combatState.IterateHookListeners())
            {
                if (model is IForgetHookHandler handler)
                {
                    await handler.BeforeForget(choiceContext, cards, source);
                    model.InvokeExecutionFinished();
                }
            }
        }

        /// <summary>
        /// 在遗忘卡牌移动之后调用所有实现了 IForgetHookHandler 的监听者。
        /// </summary>
        public static async Task AfterForget(
            ICombatState combatState,
            PlayerChoiceContext choiceContext,
            IReadOnlyList<CardModel> cards,
            CardModel? source)
        {
            if (combatState == null || cards.Count == 0) return;

            foreach (var model in combatState.IterateHookListeners())
            {
                if (model is IForgetHookHandler handler)
                {
                    await handler.AfterForget(choiceContext, cards, source);
                    model.InvokeExecutionFinished();
                }
            }
        }
    }
}