using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class EncoreNextTurnPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠层
    public override bool AllowNegative => false;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<LiveSweetPower>(); }
    }

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner) return;
        if (Amount <= 0) return;

        Flash();

        // 获得莱芜爽
        await PowerCmd.Apply<LiveSweetPower>(choiceContext, Owner, 1, Owner, null);

        // 层数 -1；当层数降为 0 时，ModifyAmount 内部会自动移除该 Power
        await PowerCmd.ModifyAmount(choiceContext, this, -1, Owner, null);
    }
}