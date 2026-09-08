
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Oblivionis
{
    public sealed class ObHairBand : ObMask
    {
        public override RelicRarity Rarity => RelicRarity.Starter;

        // 进化后每次遗忘造成6点伤害
        protected override int DamagePerForgottenCard => 6;

        // 每遗忘一张牌抽一张牌
        public override async Task AfterForget(
            PlayerChoiceContext choiceContext,
            IReadOnlyList<CardModel> cards,
            CardModel? source)
        {
            if (Owner == null || cards.Count == 0) return;
            if (cards[0].Owner != Owner) return;

            var combat = Owner.Creature?.CombatState;
            if (combat == null) return;

            await CardPileCmd.Draw(choiceContext, cards.Count, Owner);
        }
    }
}