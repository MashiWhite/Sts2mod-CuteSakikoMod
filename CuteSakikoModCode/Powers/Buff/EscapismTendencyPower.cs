using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff
{
    public sealed class EscapismTendencyPower : CuteSakikoModPower
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
        {
            await base.AfterPlayerTurnStart(choiceContext, player);
            if (player.Creature != Owner) return;

            var hand = player.PlayerCombatState?.Hand;
            if (hand == null || hand.Cards.Count == 0) return;

            var cards = hand.Cards.ToList();
            var rng = player.RunState.Rng.CombatCardSelection;
            var cardToDiscard = cards[rng.NextInt(cards.Count)];
            await CardCmd.Discard(choiceContext, cardToDiscard);
        }

        public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
        {
            await base.AfterTurnEnd(choiceContext, side);
            if (side == Owner.Side)
            {
                await CreatureCmd.GainBlock(Owner, Amount, (ValueProp)0, null);
            }
        }

        protected override IEnumerable<IHoverTip> ExtraHoverTips => [];
    }
}