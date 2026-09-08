
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using CuteSakikoMod.CuteSakikoModCode.Systems.Memory;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff
{
    public sealed class OblivionisPower : CuteSakikoModPower, IForgetHookHandler
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;
        public override bool AllowNegative => false;

        // 移除静态构造函数中的事件订阅

        public async Task BeforeForget(
            PlayerChoiceContext choiceContext,
            IReadOnlyList<CardModel> forgottenCards,
            CardModel? source)
        {
            if (forgottenCards == null || forgottenCards.Count == 0) return;

            var owner = forgottenCards[0].Owner;
            if (owner == null) return;

            var power = owner.Creature?.GetPower<OblivionisPower>();
            if (power == null || power.Amount <= 0) return;

            var combatState = owner.Creature.CombatState;
            if (combatState == null) return;

            int damagePerCard = power.Amount;

            for (int i = 0; i < forgottenCards.Count; i++)
            {
                var enemies = combatState.HittableEnemies;
                if (enemies.Count == 0) break;

                await CreatureCmd.Damage(
                    choiceContext,
                    enemies,
                    new DamageVar(damagePerCard, ValueProp.Unpowered),
                    owner.Creature,
                    null,
                    null
                );
            }
        }

        // AfterForget 不需要实现，但接口要求，可以留空或返回 Task.CompletedTask
        public Task AfterForget(
            PlayerChoiceContext choiceContext,
            IReadOnlyList<CardModel> cards,
            CardModel? source)
        {
            return Task.CompletedTask;
        }
    }
}