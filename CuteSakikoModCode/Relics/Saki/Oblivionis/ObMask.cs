
using CuteSakikoMod.CuteSakikoModCode.Character.Mujica;
using CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Starter;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using CuteSakikoMod.CuteSakikoModCode.Systems.Memory;
using MegaCrit.Sts2.Core.Saves.Runs;
using STS2RitsuLib.Interop.AutoRegistration;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Oblivionis
{
    [RegisterCharacterStarterRelic(typeof(CuteOb))]
    [RegisterTouchOfOrobasRefinement(typeof(ObHairBand))]
    public class ObMask : CuteSakiRelic, IForgetHookHandler   // 实现接口
    {
        public override RelicRarity Rarity => RelicRarity.Starter;
        private int _savedTriggeredRound = -1;

        [SavedProperty]
        protected int SavedTriggeredRound
        {
            get => _savedTriggeredRound;
            set => _savedTriggeredRound = value;
        }
        protected virtual int DamagePerForgottenCard => 3;

        // 实现 AfterForget：在卡牌移动后处理抽牌
        public virtual async Task AfterForget(
            PlayerChoiceContext choiceContext,
            IReadOnlyList<CardModel> cards,
            CardModel? source)
        {
            if (Owner == null || cards.Count == 0) return;
            if (cards[0].Owner != Owner) return;
            var combat = Owner.Creature?.CombatState;
            if (combat == null) return;

            int round = combat.RoundNumber;
            if (SavedTriggeredRound == round) return;
            SavedTriggeredRound = round;
            await CardPileCmd.Draw(choiceContext, 1, Owner);
            
        }

        // 原有伤害方法保持不变
        public virtual async Task BeforeForget(
            PlayerChoiceContext choiceContext,
            IReadOnlyList<CardModel> cards,
            CardModel? source)
        {
            if (Owner == null || cards.Count == 0) return;
            if (cards[0].Owner != Owner) return;
            var combat = Owner.Creature?.CombatState;
            if (combat == null) return;

            await ApplyDamageForForgottenCards(choiceContext, cards, source);
        }

        protected async Task ApplyDamageForForgottenCards(
            PlayerChoiceContext choiceContext,
            IReadOnlyList<CardModel> cards,
            CardModel? source)
        {
            if (cards.Count == 0) return;
            var combat = Owner?.Creature?.CombatState;
            if (combat == null) return;

            for (int i = 0; i < cards.Count; i++)
            {
                // 每次重新获取可命中敌人，避免已死敌人残留
                var enemies = combat.HittableEnemies;
                if (enemies.Count == 0) break;

                await CreatureCmd.Damage(
                    choiceContext,
                    enemies,
                    new DamageVar(DamagePerForgottenCard, ValueProp.Unpowered),
                    Owner.Creature,
                    source,   // 关键：传入触发遗忘的卡牌
                    null      // cardPlay 仍可为 null
                );
            }
        }
    }
}