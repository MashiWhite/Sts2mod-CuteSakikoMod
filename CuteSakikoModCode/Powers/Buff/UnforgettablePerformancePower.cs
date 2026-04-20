
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;


namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff
{
    public sealed class UnforgettablePerformancePower : CuteSakikoModPower
    {
        public override PowerType Type => PowerType.Buff;
        public override PowerStackType StackType => PowerStackType.Counter;

        public async Task OnChordPlayed(PlayerChoiceContext choiceContext)
        {
            if (Amount > 0)
            {
                await PlayerCmd.GainEnergy(Amount, Owner.Player);
            }
        }

        protected override IEnumerable<IHoverTip> ExtraHoverTips => [];
    }
}