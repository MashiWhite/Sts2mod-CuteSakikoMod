
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

        // 实现 BeforeForget：在卡牌移动前造成伤害
        public virtual async Task BeforeForget(
            PlayerChoiceContext choiceContext,
            IReadOnlyList<CardModel> cards,
            CardModel? source)
        {
            if (Owner == null || cards.Count == 0) return;
            if (cards[0].Owner != Owner) return;
            var combat = Owner.Creature?.CombatState;
            if (combat == null) return;

            await ApplyDamageForForgottenCards(choiceContext, cards);
        }

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
        protected async Task ApplyDamageForForgottenCards(
            PlayerChoiceContext choiceContext,
            IReadOnlyList<CardModel> cards)
        {
            if (cards.Count == 0) return;
            var combat = Owner?.Creature?.CombatState;
            if (combat == null) return;

            var enemies = combat.HittableEnemies;
            if (enemies.Count == 0) return;

            for (int i = 0; i < cards.Count; i++)
            {
                await CreatureCmd.Damage(
                    choiceContext,
                    enemies,
                    new DamageVar(DamagePerForgottenCard, ValueProp.Unpowered),
                    Owner.Creature,
                    null,
                    null
                );
            }
        }
    }
}