
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.ValueProps;


namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff
{
    public sealed class WalkHandingPower : CuteSakikoModPower
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

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