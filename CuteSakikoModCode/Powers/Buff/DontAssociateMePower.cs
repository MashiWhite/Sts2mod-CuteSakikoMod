using BaseLib.Abstracts;
using BaseLib.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Extensions;
using CuteSakikoMod.CuteSakikoModCode.Powers.Basic;
using CuteSakikoMod.CuteSakikoModCode.Powers.Debuff;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CuteSakikoMod.CuteSakikoModCode.Powers.Buff;

public sealed class DontAssociateMePower : CustomPowerModel
{
    public override string CustomPackedIconPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").PowerImagePath();

    public override string CustomBigIconPath =>
        (Id.Entry.RemovePrefix().ToLowerInvariant() + ".png").PowerImagePath();

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;
    public override bool AllowNegative => false;

    protected override IEnumerable<IHoverTip> ExtraHoverTips
    {
        get
        {
            yield return HoverTipFactory.FromPower<BreakDownPower>();
            yield return HoverTipFactory.FromPower<PressurePower>();
        }
    }

    // 在伤害计算前触发，无论最终是否格挡，只要是一次攻击，就会施加压力
    public override async Task BeforeDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource)
    {
        if (target != Owner) return;
        if (dealer == null) return;
        if (Amount <= 0) return;

        // 触发一次攻击即施加压力
        await PowerCmd.Apply<PressurePower>(dealer, Amount, Owner, cardSource);
    }

    // 在下一回合开始时（自身回合）移除能力
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner) await PowerCmd.Remove(this);
    }
}