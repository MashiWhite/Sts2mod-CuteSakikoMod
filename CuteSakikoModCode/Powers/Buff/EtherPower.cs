using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class EtherPower : CuteSakikoModPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter; // 可叠层
    public override bool AllowNegative => false;

    public override async Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side,
        IEnumerable<Creature> participants)
    {
        if (side != Owner.Side) return;
        if (Owner?.Player == null) return;

        int playCount = Amount; // 层数决定打出次数
        for (int i = 0; i < playCount; i++)
        {
            // 通过 PlayerCombatState 获取抽牌堆，防止空引用
            var drawPile = Owner.Player.PlayerCombatState?.DrawPile;
            if (drawPile == null || drawPile.Cards.Count == 0)
                break;
            await CardPileCmd.AutoPlayFromDrawPile(choiceContext, Owner.Player, 1, CardPilePosition.Top, false);
        }
    }
}