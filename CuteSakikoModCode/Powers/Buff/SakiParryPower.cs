using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class SakiParryPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠层

    public override async Task BeforeTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side != Owner.Side) return;

        var handPile = PileType.Hand.GetPile(Owner.Player);
        if (handPile == null) return;

        var hasSword = handPile.Cards.Any(c => c is KnightSword);
        if (hasSword && Amount > 0) await CreatureCmd.GainBlock(Owner, Amount, ValueProp.Move, null);
    }
}