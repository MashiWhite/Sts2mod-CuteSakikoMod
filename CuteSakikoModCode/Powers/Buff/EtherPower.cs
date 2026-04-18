using BaseLib.Abstracts;
using BaseLib.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class EtherPower : CustomPowerModel
{
    public override string CustomPackedIconPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").PowerImagePath();

    public override string CustomBigIconPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").PowerImagePath();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single; // 不可叠层
    public override bool AllowNegative => false;

    // 回合结束时，打出抽牌堆顶部的牌
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;

        // 打出抽牌堆顶部的1张牌
        await CardPileCmd.AutoPlayFromDrawPile(choiceContext, Owner.Player, 1, CardPilePosition.Top, false);
    }
}