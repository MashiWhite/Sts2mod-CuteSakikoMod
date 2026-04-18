using BaseLib.Abstracts;
using CuteSakikoMod.CuteSakikoModCode.Cards.Saki.Token;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
// 引入 Noise 的命名空间
using StringExtensions = BaseLib.Extensions.StringExtensions;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class MusicalScorePower : CustomPowerModel
{
    public override string CustomPackedIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").PowerImagePath();

    public override string CustomBigIconPath =>
        (StringExtensions.RemovePrefix(Id.Entry).ToLowerInvariant() + ".png").PowerImagePath();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    // 修改卡牌费用：只对 Noise 卡牌减少 1 点能耗
    public override bool TryModifyEnergyCostInCombat(
        CardModel card,
        decimal originalCost,
        out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        // 只对己方卡牌生效
        if (card.Owner.Creature != Owner) return false;

        // 只对 Noise 卡牌生效
        if (card is not ChordMemory) return false;

        // 减费
        modifiedCost -= 1;
        return true;
    }

    // 回合结束时移除该能力
    public override async Task AfterTurnEnd(PlayerChoiceContext choiceContext, CombatSide side)
    {
        if (side == Owner.Side) await PowerCmd.Remove(this);
    }
}