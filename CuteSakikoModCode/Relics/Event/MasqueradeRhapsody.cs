using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace CuteSakikoMod.CuteSakikoModCode.Relics.Event;

public class MasqueradeRhapsody : CuteSakikoEventRelic
{
    public override RelicRarity Rarity => RelicRarity.Event;

    // 防止重复施加触发无限递归
    private bool _isReapplying;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<PressurePower>();
        }
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource)
    {
        await base.AfterPowerAmountChanged(choiceContext, power, amount, applier, cardSource);

        // 只处理自身压力增加
        if (power is not PressurePower) return;
        if (power.Owner != Owner?.Creature) return;
        if (amount <= 0) return;
        if (_isReapplying) return;

        _isReapplying = true;
        try
        {
            // 重复施加一次相同层数的压力
            await PowerCmd.Apply<PressurePower>(
                choiceContext, Owner.Creature, amount, Owner.Creature, null);
        }
        finally
        {
            _isReapplying = false;
        }
    }
}