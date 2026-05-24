
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Saki.Rare;

public sealed class Mask : CuteSakiRelic
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get { yield return HoverTipFactory.FromPower<BreakDownPower>(); }
    }

    /// <summary>
    /// 监听任何 Power 数量变化。如果 BreakDownPower 被施加到拥有者，立即移除。
    /// </summary>
    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        Decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        // 只处理 BreakDownPower 且目标是拥有者本人
        if (power is BreakDownPower && power.Owner == Owner.Creature)
        {
            await PowerCmd.Remove(power);
            Flash();
        }
    }
}