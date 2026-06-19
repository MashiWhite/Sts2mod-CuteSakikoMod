
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class FinallyHerePower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<RanaLivePower>();
            yield return HoverTipFactory.FromPower<LiveSweetPower>();
        }
    }

    public override async Task AfterShuffle(PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner.Player) return;
        if (Amount <= 0) return;

        Flash();

        // 明确调用单个 Creature 的重载
        RanaLivePower? result = await PowerCmd.Apply<RanaLivePower>(
            choiceContext,
            Owner,           
            Amount,      
            Owner,           
            null             
        );
    }
}